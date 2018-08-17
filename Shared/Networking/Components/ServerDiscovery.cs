using System;
using System.Net;
using System.Threading;
using Duality;
using Lidgren.Network;

namespace Jazz2.Game.Multiplayer
{
    public class ServerDiscovery : IDisposable
    {
        public delegate void ServerFoundCallbackDelegate(string name, IPEndPoint endPoint, int currentPlayers, int maxPlayers, int latencyMs);

        private NetClient client;
        private Thread thread;
        private AutoResetEvent waitEvent;
        private double discoveryStartTime;

        private int port;
        private ServerFoundCallbackDelegate serverFoundAction;

        public ServerDiscovery(string appId, int port, ServerFoundCallbackDelegate serverFoundAction)
        {
            if (serverFoundAction == null) {
                throw new ArgumentNullException(nameof(serverFoundAction));
            }

            this.port = port;
            this.serverFoundAction = serverFoundAction;

            NetPeerConfiguration config = new NetPeerConfiguration(appId);
            config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
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
                discoveryStartTime = NetTime.Now;

                client.DiscoverLocalPeers(port);

                waitEvent.WaitOne(15000);
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
                    int latencyMs = MathF.RoundToInt((float)((NetTime.Now - discoveryStartTime) * 1000 * 0.5f));

                    string token = msg.ReadString();
                    int neededMajor = msg.ReadByte();
                    int neededMinor = msg.ReadByte();
                    int neededBuild = msg.ReadByte();
                    // ToDo: Check server version

                    string name = msg.ReadString();

                    byte flags = msg.ReadByte();

                    int currentPlayers = msg.ReadVariableInt32();
                    int maxPlayers = msg.ReadVariableInt32();

                    serverFoundAction(name, msg.SenderEndPoint, currentPlayers, maxPlayers, latencyMs);
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