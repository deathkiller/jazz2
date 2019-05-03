using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading;
using Jazz2.Networking.Packets;
using Lidgren.Network;

namespace Jazz2.Game.Multiplayer
{
    public class ServerDiscovery : IDisposable
    {
        public delegate void ServerFoundCallbackDelegate(Server server, bool isNew);

        public class Server
        {
            public IPEndPoint EndPoint;

            public string Name;
            public string EndPointName;
            public int CurrentPlayers;
            public int MaxPlayers;
            public int LatencyMs;

            public long LastPingTime;
        }

        private NetClient client;
        private Thread thread;
        private AutoResetEvent waitEvent;

        private int port;
        private ServerFoundCallbackDelegate serverFoundAction;

        private Dictionary<IPEndPoint, Server> foundServers;

        public ServerDiscovery(string appId, int port, ServerFoundCallbackDelegate serverFoundAction)
        {
            if (serverFoundAction == null) {
                throw new ArgumentNullException(nameof(serverFoundAction));
            }

            this.port = port;
            this.serverFoundAction = serverFoundAction;

            foundServers = new Dictionary<IPEndPoint, Server>();

            NetPeerConfiguration config = new NetPeerConfiguration(appId);
            config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
            config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
#if DEBUG
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
            client.RegisterReceivedCallback(OnMessage);
            client.Start();

            waitEvent = new AutoResetEvent(false);

            thread = new Thread(OnPeriodicDiscoveryThread);
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.Lowest;
            thread.Start();
        }

        public void Dispose()
        {
            if (client == null) {
                return;
            }

            client.UnregisterReceivedCallback(OnMessage);
            client.Shutdown(null);
            client = null;

            waitEvent.Set();

            thread.Join();

            waitEvent.Dispose();
            waitEvent = null;

            thread = null;
        }

        private void OnPeriodicDiscoveryThread()
        {
            while (client != null) {
                client.DiscoverLocalPeers(port);

                waitEvent.WaitOne(10000);
            }
        }

        private void OnMessage(object peer)
        {
            if (client == null) {
                return;
            }

            NetIncomingMessage msg = client.ReadMessage();
            switch (msg.MessageType) {
                case NetIncomingMessageType.DiscoveryResponse: {
#if DEBUG
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("    Q ");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("[" + msg.SenderEndPoint + "] " + msg.LengthBytes + " bytes");
#endif
                    bool isNew = false;

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
                    }

                    string token = msg.ReadString();
                    int neededMajor = msg.ReadByte();
                    int neededMinor = msg.ReadByte();
                    int neededBuild = msg.ReadByte();
                    // ToDo: Check server version

                    server.Name = msg.ReadString();

                    byte flags = msg.ReadByte();

                    server.CurrentPlayers = msg.ReadVariableInt32();
                    server.MaxPlayers = msg.ReadVariableInt32();

                    serverFoundAction(server, isNew);

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
                            server.LatencyMs = (int)(nowTime - server.LastPingTime) / 2 - 1;
                            if (server.LatencyMs < 0) {
                                server.LatencyMs = 0;
                            }
                            serverFoundAction(server, false);
                        }
                    }
                    break;
                }

#if DEBUG
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
}