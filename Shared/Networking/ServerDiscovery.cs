using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using Jazz2.Networking.Packets;
using Lidgren.Network;

namespace Jazz2.Game.Multiplayer
{
    public class ServerDiscovery : IDisposable
    {
        public delegate void ServerUpdatedCallbackDelegate(Server server, bool isNew);

        public class Server
        {
            public IPEndPoint EndPoint;

            public string Name;
            public string EndPointName;
            public int CurrentPlayers;
            public int MaxPlayers;
            public int LatencyMs;

            public long LastPingTime;

            public bool IsPublic;
            public bool IsLost;
        }

        public class ServerListJson
        {
            public class Server
            {
                public string name { get; set; }
                public string endpoint { get; set; }
                public int current_players { get; set; }
                public int max_players { get; set; }
            }

            public IList<Server> servers { get; set; }
        }

        private const string ServerListUrl = "http://deat.tk/jazz2/servers";

        private NetClient client;
        private Thread threadUpdate;
        private Thread threadDiscovery;
        private AutoResetEvent waitEvent;

        private int port;
        private ServerUpdatedCallbackDelegate serverUpdatedAction;

        private Dictionary<IPEndPoint, Server> foundServers;
        private Dictionary<string, IPEndPoint> publicEndPoints;
        private JsonParser jsonParser;

        public ServerDiscovery(string appId, int port, ServerUpdatedCallbackDelegate serverUpdatedAction)
        {
            if (serverUpdatedAction == null) {
                throw new ArgumentNullException(nameof(serverUpdatedAction));
            }

            this.port = port;
            this.serverUpdatedAction = serverUpdatedAction;

            foundServers = new Dictionary<IPEndPoint, Server>();
            publicEndPoints = new Dictionary<string, IPEndPoint>();
            jsonParser = new JsonParser();

            NetPeerConfiguration config = new NetPeerConfiguration(appId);
            config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
            config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
#if NETWORK_DEBUG
            config.EnableMessageType(NetIncomingMessageType.DebugMessage);
            config.EnableMessageType(NetIncomingMessageType.ErrorMessage);
            config.EnableMessageType(NetIncomingMessageType.VerboseDebugMessage);
            config.EnableMessageType(NetIncomingMessageType.WarningMessage);
#else
            config.DisableMessageType(NetIncomingMessageType.DebugMessage);
            config.DisableMessageType(NetIncomingMessageType.ErrorMessage);
            config.DisableMessageType(NetIncomingMessageType.VerboseDebugMessage);
            config.DisableMessageType(NetIncomingMessageType.WarningMessage);
#endif
            client = new NetClient(config);
            client.Start();

            waitEvent = new AutoResetEvent(false);

            threadUpdate = new Thread(OnHandleMessagesThread);
            threadUpdate.IsBackground = true;
            threadUpdate.Start();

            threadDiscovery = new Thread(OnPeriodicDiscoveryThread);
            threadDiscovery.IsBackground = true;
            threadDiscovery.Priority = ThreadPriority.Lowest;
            threadDiscovery.Start();
        }

        public void Dispose()
        {
            if (threadUpdate == null && threadDiscovery == null) {
                return;
            }

            threadUpdate = null;
            threadDiscovery = null;

            client.Shutdown(null);

            waitEvent.Set();
        }

        private void OnHandleMessagesThread()
        {
            while (threadUpdate != null) {
                client.MessageReceivedEvent.WaitOne();

                NetIncomingMessage msg;
                while (client.ReadMessage(out msg)) {
                    switch (msg.MessageType) {
                        case NetIncomingMessageType.DiscoveryResponse: {
#if NETWORK_DEBUG
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write("    Q ");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine("[" + msg.SenderEndPoint + "] " + msg.LengthBytes + " bytes");
#endif
                            bool isNew;
                            Server server;
                            if (!foundServers.TryGetValue(msg.SenderEndPoint, out server)) {
                                string endPointName;
                                if (msg.SenderEndPoint.Address.IsIPv4MappedToIPv6) {
                                    endPointName = msg.SenderEndPoint.Address.MapToIPv4().ToString();
                                } else {
                                    endPointName = msg.SenderEndPoint.Address.ToString();
                                }
                                endPointName += ":" + msg.SenderEndPoint.Port.ToString(CultureInfo.InvariantCulture);

                                server = new Server {
                                    EndPoint = msg.SenderEndPoint,
                                    EndPointName = endPointName,
                                    LatencyMs = -1
                                };

                                foundServers[msg.SenderEndPoint] = server;
                                isNew = true;
                            } else {
                                if (server.IsPublic) {
                                    break;
                                }

                                isNew = false;
                            }

                            string token = msg.ReadString();
                            int neededMajor = msg.ReadByte();
                            int neededMinor = msg.ReadByte();
                            int neededBuild = msg.ReadByte();
                            // ToDo: Check server version

                            server.Name = msg.ReadString();
                            server.IsLost = false;

                            byte flags = msg.ReadByte();

                            server.CurrentPlayers = msg.ReadVariableInt32();
                            server.MaxPlayers = msg.ReadVariableInt32();

                            serverUpdatedAction(server, isNew);

                            // Send ping request
                            server.LastPingTime = (long)(NetTime.Now * 1000);

                            NetOutgoingMessage m = client.CreateMessage();
                            m.Write(PacketTypes.Ping);
                            client.SendUnconnectedMessage(m, server.EndPoint);
                            break;
                        }

                        case NetIncomingMessageType.UnconnectedData: {
                            if (msg.LengthBytes == 1 && msg.ReadByte() == PacketTypes.Ping) {
                                long nowTime = (long)(NetTime.Now * 1000);

                                Server server;
                                if (foundServers.TryGetValue(msg.SenderEndPoint, out server)) {
                                    server.IsLost = false;
                                    server.LatencyMs = (int)(nowTime - server.LastPingTime) / 2 - 1;
                                    if (server.LatencyMs < 0) {
                                        server.LatencyMs = 0;
                                    }
                                    serverUpdatedAction(server, false);
                                }
                            }
                            break;
                        }

#if NETWORK_DEBUG
                        case NetIncomingMessageType.VerboseDebugMessage:
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write("    D ");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine(msg.ReadString());
                            break;
                        case NetIncomingMessageType.DebugMessage:
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("    D ");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine(msg.ReadString());
                            break;
                        case NetIncomingMessageType.WarningMessage:
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write("    W ");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine(msg.ReadString());
                            break;
                        case NetIncomingMessageType.ErrorMessage:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("    E ");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine(msg.ReadString());
                            break;
#endif
                    }

                    client.Recycle(msg);
                }
            }

            client = null;

            Debug.WriteLine("ServerDiscovery: OnHandleMessagesThread exited!");
        }

        private void OnPeriodicDiscoveryThread()
        {
            double discoverPublicTime = 0;

            while (threadDiscovery != null) {
                // Discover new public servers every 30 seconds
                if ((NetTime.Now - discoverPublicTime) < 30) {
                    discoverPublicTime = NetTime.Now;

                    DiscoverPublicServers();
                }

                // Discover new local servers
                DiscoverLocalServers();

                // Wait
                waitEvent.WaitOne(10000);
            }

            waitEvent.Dispose();
            waitEvent = null;

            Debug.WriteLine("ServerDiscovery: OnPeriodicDiscoveryThread exited!");
        }

        private void DiscoverPublicServers()
        {
            string deviceId;
#if __ANDROID__
            try {
                deviceId = global::Android.Provider.Settings.Secure.GetString(Android.MainActivity.Current.ContentResolver, global::Android.Provider.Settings.Secure.AndroidId);
                if (deviceId == null) {
                    deviceId = "";
                }
            } catch {
                deviceId = "";
            }

            deviceId += "|Android " + global::Android.OS.Build.VERSION.Release;
#else
            try {
                deviceId = Environment.MachineName;
                if (deviceId == null) {
                    deviceId = "";
                }
            } catch {
                deviceId = "";
            }

            deviceId += "|" + Environment.OSVersion.ToString();
#endif

            deviceId = Convert.ToBase64String(Encoding.UTF8.GetBytes(deviceId))
                            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

            ServerListJson json;
            try {
                string currentVersion = App.AssemblyVersion;

                WebClient http = new WebClient();
                http.Encoding = Encoding.UTF8;
                http.Headers["User-Agent"] = App.AssemblyTitle;

                string content = http.DownloadString(ServerListUrl + "?fetch&v=" + currentVersion + "&d=" + deviceId);
                if (content == null) {
                    return;
                }

                json = jsonParser.Parse<ServerListJson>(content);
            } catch {
                // Nothing to do...
                return;
            }

            // Remove lost local servers
            foreach (KeyValuePair<IPEndPoint, Server> pair in foundServers) {
                if (!pair.Value.IsPublic) {
                    continue;
                }

                if (pair.Value.IsLost) {
                    pair.Value.LatencyMs = -1;
                    serverUpdatedAction(pair.Value, false);
                } else {
                    pair.Value.IsLost = true;
                }
            }

            // Process server list
            if (json.servers != null) {
                foreach (ServerListJson.Server s in json.servers) {

                    IPEndPoint endPoint;
                    if (!publicEndPoints.TryGetValue(s.endpoint, out endPoint)) {
                        int idx = s.endpoint.LastIndexOf(':');
                        if (idx == -1) {
                            publicEndPoints[s.endpoint] = null;
                            continue;
                        }

                        int port;
                        if (!int.TryParse(s.endpoint.Substring(idx + 1), NumberStyles.Any, CultureInfo.InvariantCulture, out port)) {
                            publicEndPoints[s.endpoint] = null;
                            continue;
                        }

                        try {
                            IPAddress ip = NetUtility.Resolve(s.endpoint.Substring(0, idx));
                            endPoint = new IPEndPoint(ip, port);
                        } catch {
                            endPoint = null;
                        }
                       
                        publicEndPoints[s.endpoint] = endPoint;
                    }

                    if (endPoint == null) {
                        // Endpoint cannot be parsed, skip it
                        continue;
                    }

                    bool isNew;
                    Server server;
                    if (!foundServers.TryGetValue(endPoint, out server)) {
                        string endPointName;
                        if (endPoint.Address.IsIPv4MappedToIPv6) {
                            endPointName = endPoint.Address.MapToIPv4().ToString();
                        } else {
                            endPointName = endPoint.Address.ToString();
                        }
                        endPointName += ":" + endPoint.Port.ToString(CultureInfo.InvariantCulture);

                        server = new Server {
                            EndPoint = endPoint,
                            EndPointName = endPointName,
                            LatencyMs = -1,
                            IsPublic = true,
                            IsLost = true
                        };

                        foundServers[endPoint] = server;
                        isNew = true;
                    } else {
                        isNew = false;
                        server.IsPublic = true;
                    }

                    server.Name = s.name;

                    server.CurrentPlayers = s.current_players;
                    server.MaxPlayers = s.max_players;

                    serverUpdatedAction(server, isNew);

                    // Send ping request
                    server.LastPingTime = (long)(NetTime.Now * 1000);

                    NetOutgoingMessage m = client.CreateMessage();
                    m.Write(PacketTypes.Ping);
                    client.SendUnconnectedMessage(m, server.EndPoint);
                }
            }
        }

        private void DiscoverLocalServers()
        {
            // Remove lost local servers
            foreach (KeyValuePair<IPEndPoint, Server> pair in foundServers) {
                if (pair.Value.IsPublic) {
                    continue;
                }

                if (pair.Value.IsLost) {
                    pair.Value.LatencyMs = -1;
                    serverUpdatedAction(pair.Value, false);
                } else {
                    pair.Value.IsLost = true;
                }
            }

            // Discover new local servers
            client.DiscoverLocalPeers(port);
        }
    }
}