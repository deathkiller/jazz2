using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Threading;
using Lidgren.Network;

namespace Jazz2.Server
{
    internal static class App
    {
        private struct ServerDescription
        {
            public string Name;
            public int CurrentPlayers;
            public int MaxPlayers;
            public IPEndPoint InternalEndpoint;

            public double LastHeartbeat;
        }

        private class Player
        {

        }

        private const string token = "J²";

        private static string name;
        private static int port, maxPlayers;

        //private static IPEndPoint masterServerEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 10667);
        private static IPEndPoint masterServerEndpoint = null;

        private static Thread threadGame;
        private static ServerConnection server;
        private static byte neededMajor, neededMinor, neededBuild;

        private static Dictionary<byte, Action<NetIncomingMessage, bool>> callbacks;
        private static Dictionary<NetConnection, Player> players;
        private static Dictionary<IPEndPoint, ServerDescription> registeredHosts;

        private static double lastRegisteredToMaster;
        private static int lastGameLoadMs;

        private static void Main(string[] args)
        {
            bool isMasterServer = TryRemoveArg(ref args, "/master");

            if (!TryRemoveArg(ref args, "/port:", out port)) {
                port = (isMasterServer ? 10667 : 10666);
            }

            if (!isMasterServer) {
                if (!TryRemoveArg(ref args, "/name:", out name) || string.IsNullOrWhiteSpace(name)) {
                    name = "Unnamed server";
                }

                if (!TryRemoveArg(ref args, "/players:", out maxPlayers)) {
                    maxPlayers = 64;
                }
            }
            
            // Initialization
            Version v = Assembly.GetEntryAssembly().GetName().Version;
            neededMajor = (byte)v.Major;
            neededMinor = (byte)v.Minor;
            neededBuild = (byte)v.Build;

            if (isMasterServer) {
                ReportProgress("Starting master server...", 0);
                ReportProgress("Port: " + port);

                registeredHosts = new Dictionary<IPEndPoint, ServerDescription>();

                server = new ServerConnection(token, port, 0);
                server.MessageReceived += OnMasterMessageReceived;
            } else {
                ReportProgress("Starting server...", 0);
                ReportProgress("Port: " + port);
                ReportProgress("Server Name: " + name);
                ReportProgress("Max. Players: " + maxPlayers);

                callbacks = new Dictionary<byte, Action<NetIncomingMessage, bool>>();
                players = new Dictionary<NetConnection, Player>();

                server = new ServerConnection(token, port, maxPlayers);
                server.MessageReceived += OnMessageReceived;
                server.DiscoveryRequest += OnDiscoveryRequest;
                server.ClientConnected += OnClientConnected;

                // ToDo: Renew it periodically
                RegisterToMasterServer();
            }

            // Create game loop (~60fps)
            threadGame = new Thread(OnGameLoop);
            threadGame.IsBackground = true;
            threadGame.Start();

            ReportProgress("Ready!", 100);
            Console.WriteLine();

            // Processing of console commands
            while (true) {
                string command = Console.ReadLine();
                if (!string.IsNullOrEmpty(command)) {
                    switch (command) {
                        case "quit":
                        case "exit":
                            goto Finalize;

                        default:
                            Console.WriteLine("Unknown command: " + command);
                            break;

                    }
                }
            }

        Finalize:
            // Shutdown
            Console.WriteLine();
            ReportProgress("Closing...");

            if (isMasterServer) {
                server.MessageReceived -= OnMasterMessageReceived;
            } else {
                server.ClientConnected -= OnClientConnected;
                server.MessageReceived -= OnMessageReceived;
                server.DiscoveryRequest -= OnDiscoveryRequest;
            }

            //ClearCallbacks();

            Thread threadGame_ = threadGame;
            threadGame = null;
            threadGame_.Join();

            server.Close();
        }

        private static void OnGameLoop()
        {
            Stopwatch sw = new Stopwatch();

            while (threadGame != null) {
                sw.Restart();

                // ToDo: Update components

                sw.Stop();

                lastGameLoadMs = (int)sw.ElapsedMilliseconds;
                int sleepMs = 1000 / 60 - lastGameLoadMs;
                if (sleepMs > 0) {
                    Thread.Sleep(sleepMs);
                }
            }
        }

        #region Startup
        public static bool TryRemoveArg(ref string[] args, string arg)
        {
            for (int i = 0; i < args.Length; i++) {
                if (string.Compare(args[i], arg, StringComparison.OrdinalIgnoreCase) == 0) {
                    List<string> list = new List<string>(args);
                    list.RemoveAt(i);
                    args = list.ToArray();
                    return true;
                }
            }

            return false;
        }

        public static bool TryRemoveArg(ref string[] args, string argPrefix, out string argSuffix)
        {
            for (int i = 0; i < args.Length; i++) {
                if (args[i].StartsWith(argPrefix, StringComparison.OrdinalIgnoreCase)) {
                    argSuffix = args[i].Substring(argPrefix.Length);

                    List<string> list = new List<string>(args);
                    list.RemoveAt(i);
                    args = list.ToArray();
                    return true;
                }
            }

            argSuffix = null;
            return false;
        }

        public static bool TryRemoveArg(ref string[] args, string argPrefix, out int argSuffix)
        {
            string suffix;
            if (TryRemoveArg(ref args, argPrefix, out suffix) && int.TryParse(suffix, out argSuffix)) {
                return true;
            }

            argSuffix = 0;
            return false;
        }

        public static void ReportProgress(string text, int progress = -1)
        {
            if (progress < 0) {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("    ˙ ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(text);
            } else {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write((progress + "%").PadLeft(5) + " ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(text);
            }
        }
        #endregion

        #region Events
        private static void OnClientConnected(ClientConnectedEventArgs args)
        {
            // Check header of connection request
            //string identifier = args.Message.ReadString();
            //if (identifier != token) {
            //    Console.WriteLine("        - " + "Bad identifier!");
            //    return;
            //}

            byte major = args.Message.ReadByte();
            byte minor = args.Message.ReadByte();
            byte build = args.Message.ReadByte();
            if (major < neededMajor || (major == neededMajor && (minor < neededMinor || (major == neededMajor && build < neededBuild)))) {
                Console.WriteLine("        - " + "Incompatible version!");
                return;
            }

            args.Allow = true;

            players[args.Message.SenderConnection] = new Player {

            };
        }

        private static void OnMessageReceived(MessageReceivedEventArgs args)
        {
            if (args.IsUnconnected) {
                string identifier = args.Message.ReadString();
                if (identifier != token) {
                    Console.WriteLine("        - " + "Bad identifier!");
                    return;
                }

                byte major = args.Message.ReadByte();
                byte minor = args.Message.ReadByte();
                byte build = args.Message.ReadByte();
                if (major < neededMajor || (major == neededMajor && (minor < neededMinor || (major == neededMajor && build < neededBuild)))) {
                    Console.WriteLine("        - Incompatible version!");
                    return;
                }
            }

            byte type = args.Message.ReadByte();

            Action<NetIncomingMessage, bool> callback;
            if (callbacks.TryGetValue(type, out callback)) {
                callback(args.Message, args.IsUnconnected);
            } else {
                Console.WriteLine("        - Unknown packet type!");
            }
        }

        private static void OnMasterMessageReceived(MessageReceivedEventArgs args)
        {
            // All messages will be unconnected here...
            //if (args.IsUnconnected) {
                string identifier = args.Message.ReadString();
                if (identifier != token) {
                    Console.WriteLine("        - Bad identifier!");
                    return;
                }

                byte major = args.Message.ReadByte();
                byte minor = args.Message.ReadByte();
                byte build = args.Message.ReadByte();
                if (major < neededMajor || (major == neededMajor && (minor < neededMinor || (major == neededMajor && build < neededBuild)))) {
                    Console.WriteLine("        - Incompatible version!");
                    return;
                }
            //}

            byte type = args.Message.ReadByte();

            switch (type) {
                case 0: { // Server List Request
                    // ToDo: Cleanup server list (timeout)

                    NetOutgoingMessage m = server.CreateMessage(registeredHosts.Count * 32);
                    m.WriteVariableInt32(registeredHosts.Count);
                    foreach (var host in registeredHosts) {
                        m.Write(host.Value.Name);
                        m.Write(host.Key);
                        m.Write(host.Value.InternalEndpoint);
                        m.WriteVariableInt32(host.Value.CurrentPlayers);
                        m.WriteVariableInt32(host.Value.MaxPlayers);
                    }
                    server.SendUnconnected(m, args.Message.SenderEndPoint);
                    break;
                }

                case 1: { // Register Host
                    string name = args.Message.ReadString();
                    int currentPlayers = args.Message.ReadVariableInt32();
                    int maxPlayers = args.Message.ReadVariableInt32();
                    IPEndPoint internalEndpoint = args.Message.ReadIPEndPoint();

                    if (registeredHosts.ContainsKey(args.Message.SenderEndPoint)) {
                        Console.WriteLine("New host (" + args.Message.SenderEndPoint + ") registered!");
                    }

                    registeredHosts[args.Message.SenderEndPoint] = new ServerDescription {
                        Name = name,
                        CurrentPlayers = currentPlayers,
                        MaxPlayers = maxPlayers,
                        InternalEndpoint = internalEndpoint,
                        LastHeartbeat = NetTime.Now
                    };
                    break;
                }

                case 2: { // Request Introduction
                    IPEndPoint serverEndpoint = args.Message.ReadIPEndPoint();
                    IPEndPoint clientInternalEndpoint = args.Message.ReadIPEndPoint();

                    ServerDescription description;
                    if (registeredHosts.TryGetValue(serverEndpoint, out description)) {
                        server.Introduce(
                            description.InternalEndpoint,
                            serverEndpoint,
                            clientInternalEndpoint,
                            args.Message.SenderEndPoint,
                            null // Request Token
                        );
                    } else {
                        Console.WriteLine("Client requested introduction to non-listed host!");
                    }
                    break;
                }
            }
        }

        private static void OnDiscoveryRequest(DiscoveryRequestEventArgs args)
        {
            NetOutgoingMessage msg = server.CreateMessage(64);

            // Header for unconnected message
            msg.Write(token);
            msg.Write(neededMajor);
            msg.Write(neededMinor);
            msg.Write(neededBuild);

            // Message
            msg.Write(name);

            byte flags = 0;
            // ToDo: Password protected servers
            msg.Write((byte)flags);

            msg.WriteVariableInt32(server.ConnectionsCount);
            msg.WriteVariableInt32(maxPlayers);

            args.Message = msg;
        }
        #endregion

        #region Callbacks
        public static void RegisterCallback<T>(PacketCallback<T> callback) where T : struct, IClientPacket
        {
            byte type = (new T().Type);
            callbacks[type] = (msg, isUnconnected) => ProcessCallback(msg, isUnconnected, callback);
        }

        public static void RemoveCallback<T>() where T : struct, IClientPacket
        {
            byte type = (new T().Type);
            callbacks.Remove(type);
        }

        private static void ClearCallbacks()
        {
            callbacks.Clear();
        }

        private static void ProcessCallback<T>(NetIncomingMessage msg, bool isUnconnected, PacketCallback<T> callback) where T : struct, IClientPacket
        {
            T packet = default(T);
            if (isUnconnected && !packet.SupportsUnconnected) {
#if DEBUG
                Console.WriteLine("        - Packet<" + typeof(T).Name + "> not allowed for unconnected clients!");
#endif
                return;
            }

#if DEBUG
            Console.WriteLine("        - Packet<" + typeof(T).Name + ">");
#endif

            packet.SenderConnection = msg.SenderConnection;
            packet.Read(msg);
            callback(ref packet);
        }
        #endregion

        #region Messages
        public static bool Send<T>(T packet, int capacity, NetConnection recipient, NetDeliveryMethod method) where T : struct, IServerPacket
        {
            NetOutgoingMessage msg = server.CreateMessage(capacity);
            msg.Write((byte)packet.Type);
            packet.Write(msg);
            NetSendResult result = server.Send(msg, recipient, method);

#if DEBUG
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Debug: ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Send<" + typeof(T).Name + ">  " + msg.LengthBytes + " bytes");
#endif
            return (result == NetSendResult.Sent || result == NetSendResult.Queued);
        }

        public static bool Send<T>(T packet, int capacity, List<NetConnection> recipients, NetDeliveryMethod method) where T : struct, IServerPacket
        {
            NetOutgoingMessage msg = server.CreateMessage(capacity);
            msg.Write((byte)packet.Type);
            packet.Write(msg);

            if (recipients.Count > 0) {
#if DEBUG
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Debug: ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Send<" + typeof(T).Name + ">  " + msg.LengthBytes + " bytes, " + recipients.Count + " recipients");
#endif
                server.Send(msg, recipients, method, 0);
                return true;
            } else {
                return false;
            }
        }
        #endregion

        #region Master Server
        public static void RegisterToMasterServer()
        {
            if (masterServerEndpoint != null && NetTime.Now > lastRegisteredToMaster + 180) {

                NetOutgoingMessage msg = server.CreateMessage();

                // Header for unconnected message
                msg.Write(token);
                msg.Write(neededMajor);
                msg.Write(neededMinor);
                msg.Write(neededBuild);

                // Message
                // ToDo: Hardcoded constant
                msg.Write((byte)1 /*Register Host*/);

                msg.Write(name);
                msg.WriteVariableInt32(server.ConnectionsCount);
                msg.WriteVariableInt32(maxPlayers);

                IPAddress mask;
                IPAddress address = NetUtility.GetMyAddress(out mask);
                msg.Write(new IPEndPoint(address, port));

                Console.WriteLine("Sending registration to master server");

                server.SendUnconnected(msg, masterServerEndpoint);

                lastRegisteredToMaster = (float)NetTime.Now;
            }
        }
        #endregion
    }
}