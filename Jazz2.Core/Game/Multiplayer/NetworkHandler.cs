#if MULTIPLAYER

using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using Jazz2.NetworkPackets;
using Lidgren.Network;

namespace Jazz2.Game.Multiplayer
{
    public class NetworkHandler
    {
        private NetClient client;
        private Thread threadUpdate;

        private Dictionary<byte, Action<NetIncomingMessage>> callbacks;

        public NetworkHandler(string appId)
        {
            callbacks = new Dictionary<byte, Action<NetIncomingMessage>>();

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
            NetOutgoingMessage message = client.CreateMessage(3);
            Version v = Assembly.GetEntryAssembly().GetName().Version;
            message.Write((byte)v.Major);
            message.Write((byte)v.Minor);
            message.Write((byte)v.Build);

            client.Connect(host, message);
        }

        public void Close()
        {
            if (client == null) {
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
#if DEBUG__
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.Write("    R ");
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.WriteLine("[" + msg.SenderEndPoint + "] " + msg.LengthBytes + " bytes");
#endif

                                byte type = msg.ReadByte();

                                Action<NetIncomingMessage> callback;
                                if (callbacks.TryGetValue(type, out callback)) {
                                    callback(msg);
                                } else {
                                    Console.WriteLine("        - Unknown packet type!");
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
            } catch (ThreadAbortException) {
                // Client is stopped
            }
        }

        #region Messages

        public bool Send<T>(T packet, int capacity, NetDeliveryMethod method, int channel) where T : struct, IClientPacket
        {
            NetOutgoingMessage msg = client.CreateMessage(capacity);
            msg.Write((byte)packet.Type);
            packet.Write(msg);
            NetSendResult result = client.SendMessage(msg, method, channel);

#if DEBUG__
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Debug: ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Send<" + typeof(T).Name + ">  " + msg.LengthBytes + " bytes");
#endif
            return (result == NetSendResult.Sent || result == NetSendResult.Queued);
        }
        #endregion

        #region Callbacks
        public void RegisterCallback<T>(PacketCallback<T> callback) where T : struct, IServerPacket
        {
            byte type = (new T().Type);
            callbacks[type] = (msg) => ProcessCallback(msg, callback);
        }

        public void RemoveCallback<T>() where T : struct, IServerPacket
        {
            byte type = (new T().Type);
            callbacks.Remove(type);
        }

        private void ClearCallbacks()
        {
            callbacks.Clear();
        }

        private static void ProcessCallback<T>(NetIncomingMessage msg, PacketCallback<T> callback) where T : struct, IServerPacket
        {
            T packet = default(T);
#if DEBUG__
            Console.WriteLine("        - Packet<" + typeof(T).Name + ">");
#endif

            packet.SenderConnection = msg.SenderConnection;
            packet.Read(msg);
            callback(ref packet);
        }
        #endregion
    }
}

#endif