#if MULTIPLAYER

using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using Lidgren.Network;

namespace Jazz2.Game.Multiplayer
{
    public class NetworkHandler
    {
        private NetClient client;
        private Thread threadUpdate;

        public NetworkHandler(string appId)
        {
            //callbacks = new Dictionary<byte, Action<NetIncomingMessage>>(32);

            NetPeerConfiguration config = new NetPeerConfiguration(appId);
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
            client.Start();

            threadUpdate = new Thread(OnMessage);
            threadUpdate.IsBackground = true;
            threadUpdate.Start();
        }

        public void Connect(string host, int port)
        {
            Connect(new IPEndPoint(NetUtility.Resolve(host), port));
        }

        public void Connect(IPEndPoint host)
        {
            client.Connect(host);
        }

        public void Close()
        {
            if (server == null) {
                return;
            }

            threadUpdate.Abort();
            threadUpdate = null;

            client.Shutdown("Client is disconnecting");
            client = null;
        }

        private void OnMessage()
        {
            try {
                while (true) {
                    client.MessageReceivedEvent.WaitOne();

                    NetIncomingMessage msg;
                    while (client.ReadMessage(out msg)) {

                        switch (msg.MessageType) {
                            case NetIncomingMessageType.StatusChanged:
                                NetConnectionStatus status = (NetConnectionStatus)msg.ReadByte();
#if DEBUG
                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                Console.Write("    S ");
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.WriteLine("[" + msg.SenderEndPoint + "] " + status);
#endif
                                
                                break;

                            case NetIncomingMessageType.Data: {
#if DEBUG
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.Write("    R ");
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.WriteLine("[" + msg.SenderEndPoint + "] " + msg.LengthBytes + " bytes");
#endif

                                MessageReceivedEventArgs args = new MessageReceivedEventArgs(msg, false);
                                MessageReceived?.Invoke(args);

                                break;
                            }

                            case NetIncomingMessageType.UnconnectedData: {
#if DEBUG
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.Write("    R ");
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.WriteLine("Unconnected [" + msg.SenderEndPoint + "] " + msg.LengthBytes + " bytes");
#endif

                                MessageReceivedEventArgs args = new MessageReceivedEventArgs(msg, true);
                                MessageReceived?.Invoke(args);

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

                        server.Recycle(msg);
                    }
                }
            } catch (ThreadAbortException) {
                // Client is stopped
            }
        }
    }
}

#endif