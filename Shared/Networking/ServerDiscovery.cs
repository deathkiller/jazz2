#if !SERVER

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using Jazz2.Networking;
using Lidgren.Network;

namespace Jazz2.Game
{
    public sealed class ServerDiscovery : IDisposable
    {
        public delegate void ServerUpdatedCallbackDelegate(Server server, bool isNew);

        public class Server
        {
            public IPEndPoint ActiveEndPoint;
            public List<IPEndPoint> PublicEndPointList;

            public string UniqueIdentifier;
            public string Name;
            public string ActiveEndPointName;
            public int CurrentPlayers;
            public int MaxPlayers;
            public int LatencyMs;

            public long LastPingTime;

            public bool IsLost;
        }

        public class ServerListJson
        {
            public class Server
            {
                // Name
                public string n { get; set; }
                // Unique Identifier
                public string u { get; set; }
                // Endpoints
                public string e { get; set; }
                // Current Players
                public int c { get; set; }
                // Max Players
                public int m { get; set; }
            }

            // Servers
            public IList<Server> s { get; set; }
        }

        private const string ServerListUrl = "http://deat.tk/jazz2/servers";

        private NetClient client;
        private Thread threadUpdate;
        private Thread threadDiscovery;
        private AutoResetEvent waitEvent;

        private int port;
        private ServerUpdatedCallbackDelegate serverUpdatedAction;

        private Dictionary<string, Server> foundServers;
        private Dictionary<string, List<IPEndPoint>> publicEndPoints;
        private JsonParser jsonParser;

        public ServerDiscovery(string appId, int port, ServerUpdatedCallbackDelegate serverUpdatedAction)
        {
            if (serverUpdatedAction == null) {
                throw new ArgumentNullException(nameof(serverUpdatedAction));
            }

            this.port = port;
            this.serverUpdatedAction = serverUpdatedAction;

            foundServers = new Dictionary<string, Server>();
            publicEndPoints = new Dictionary<string, List<IPEndPoint>>();
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
                            string token = msg.ReadString();
                            int neededMajor = msg.ReadByte();
                            int neededMinor = msg.ReadByte();
                            int neededBuild = msg.ReadByte();
                            // ToDo: Check server version

                            string uniqueIdentifier = msg.ReadString();

                            bool isNew;
                            Server server;
                            if (!foundServers.TryGetValue(uniqueIdentifier, out server)) {
                                IPEndPoint endPoint = msg.SenderEndPoint;
                                if (endPoint.Address.IsIPv4MappedToIPv6) {
                                    endPoint.Address = endPoint.Address.MapToIPv4();
                                }

                                server = new Server {
                                    UniqueIdentifier = uniqueIdentifier,
                                    ActiveEndPoint = endPoint,
                                    ActiveEndPointName = msg.SenderEndPoint.Address.ToString() + ":" + msg.SenderEndPoint.Port.ToString(CultureInfo.InvariantCulture),
                                    LatencyMs = -1
                                };

                                foundServers[uniqueIdentifier] = server;
                                isNew = true;
                            } else {
                                if (server.PublicEndPointList != null) {
                                    break;
                                }

                                isNew = false;
                            }

                            server.Name = msg.ReadString();
                            server.IsLost = false;

                            byte flags = msg.ReadByte();

                            server.CurrentPlayers = msg.ReadVariableInt32();
                            server.MaxPlayers = msg.ReadVariableInt32();

                            serverUpdatedAction(server, isNew);

                            // Send ping request
                            server.LastPingTime = (long)(NetTime.Now * 1000);

                            NetOutgoingMessage m = client.CreateMessage();
                            m.Write(SpecialPacketTypes.Ping);
                            client.SendUnconnectedMessage(m, server.ActiveEndPoint);
                            break;
                        }

                        case NetIncomingMessageType.UnconnectedData: {
                            if (msg.LengthBytes > 1 && msg.PeekByte() == SpecialPacketTypes.Ping) {
                                long nowTime = (long)(NetTime.Now * 1000);

                                msg.ReadByte(); // Already checked
                                string uniqueIdentifier = msg.ReadString();

                                Server server;
                                if (foundServers.TryGetValue(uniqueIdentifier, out server)) {
                                    bool isUpdated;
                                    if (server.PublicEndPointList != null) {
                                        isUpdated = false;
                                        foreach (IPEndPoint endpoint in server.PublicEndPointList) {
                                            if (endpoint.Equals(msg.SenderEndPoint)) {
                                                server.ActiveEndPoint = endpoint;
                                                isUpdated = true;

                                                server.ActiveEndPointName = endpoint.Address.ToString() + ":" + endpoint.Port.ToString(CultureInfo.InvariantCulture);
                                                break;
                                            }
                                        }
                                    } else {
                                        isUpdated = true;
                                    }

                                    if (isUpdated) {
                                        server.IsLost = false;
                                        server.LatencyMs = (int)(nowTime - server.LastPingTime) / 2 - 1;
                                        if (server.LatencyMs < 0) {
                                            server.LatencyMs = 0;
                                        }

                                        serverUpdatedAction(server, false);
                                    }
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
#if PLATFORM_ANDROID
            try {
                deviceId = global::Android.Provider.Settings.Secure.GetString(Android.MainActivity.Current.ContentResolver, global::Android.Provider.Settings.Secure.AndroidId);
                if (deviceId == null) {
                    deviceId = "";
                }
            } catch {
                deviceId = "";
            }

            deviceId += "|Android " + global::Android.OS.Build.VERSION.Release + "|";
                
            try {
                string device = (string.IsNullOrEmpty(Build.Model) ? Build.Manufacturer : (Build.Model.StartsWith(Build.Manufacturer) ? Build.Model : Build.Manufacturer + " " + Build.Model));

                if (device == null) {
                    device = "";
                } else if (device.Length > 1) {
                    device = char.ToUpper(device[0]) + device.Substring(1);
                }
                
                deviceId += device;
            } catch {
            }
#else
            try {
                deviceId = Environment.MachineName;
                if (deviceId == null) {
                    deviceId = "";
                }
            } catch {
                deviceId = "";
            }

            deviceId += "|" + Environment.OSVersion.ToString() + "|";
#endif

            deviceId = Convert.ToBase64String(Encoding.UTF8.GetBytes(deviceId))
                            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

            ServerListJson json;
            try {
                string currentVersion = App.AssemblyVersion;

                WebClient http = new WebClient();
                http.Encoding = Encoding.UTF8;
                http.Headers["User-Agent"] = "Jazz2 Resurrection";

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
            foreach (KeyValuePair<string, Server> pair in foundServers) {
                if (pair.Value.PublicEndPointList == null) {
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
            if (json.s != null) {
                foreach (ServerListJson.Server s in json.s) {

                    List<IPEndPoint> endPoints;
                    if (!publicEndPoints.TryGetValue(s.u, out endPoints)) {
                        string[] endPointsRaw = s.e.Split('|');
                        endPoints = new List<IPEndPoint>(endPointsRaw.Length);
                        for (int i = 0; i < endPointsRaw.Length; i++) {
                            int idx = endPointsRaw[i].LastIndexOf(':');
                            if (idx == -1) {
                                continue;
                            }

                            int port;
                            if (!int.TryParse(endPointsRaw[i].Substring(idx + 1), NumberStyles.Any, CultureInfo.InvariantCulture, out port)) {
                                continue;
                            }

                            try {
                                IPAddress ip = NetUtility.Resolve(endPointsRaw[i].Substring(0, idx));

                                if (ip.IsIPv4MappedToIPv6) {
                                    ip = ip.MapToIPv4();
                                }

                                endPoints.Add(new IPEndPoint(ip, port));
                            } catch {
                                // Nothing to do...
                            }
                        }

                        publicEndPoints[s.e] = endPoints;
                    }

                    if (endPoints.Count == 0) {
                        // Endpoints cannot be parsed, skip this server
                        continue;
                    }

                    bool isNew;
                    Server server;
                    if (!foundServers.TryGetValue(s.u, out server)) {
                        server = new Server {
                            UniqueIdentifier = s.u,
                            ActiveEndPointName = endPoints[0].Address.ToString() + ":" + endPoints[0].Port.ToString(CultureInfo.InvariantCulture),
                            LatencyMs = -1,
                            IsLost = true
                        };

                        foundServers[s.u] = server;
                        isNew = true;
                    } else {
                        isNew = false;
                    }

                    server.PublicEndPointList = endPoints;
                    server.Name = s.n;

                    server.CurrentPlayers = s.c;
                    server.MaxPlayers = s.m;

                    serverUpdatedAction(server, isNew);

                    // Send ping request
                    server.LastPingTime = (long)(NetTime.Now * 1000);

                    NetOutgoingMessage m = client.CreateMessage();
                    m.Write(SpecialPacketTypes.Ping);
                    client.SendUnconnectedMessage(m, endPoints);
                }
            }
        }

        private void DiscoverLocalServers()
        {
            // Remove lost local servers
            foreach (KeyValuePair<string, Server> pair in foundServers) {
                if (pair.Value.PublicEndPointList != null) {
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

#endif