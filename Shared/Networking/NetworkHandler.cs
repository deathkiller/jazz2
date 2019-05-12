using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Jazz2.Networking;
using Jazz2.Networking.Packets;
using Lidgren.Network;

namespace Jazz2.Game.Multiplayer
{
    public class NetworkHandler
    {
        private NetClient client;
        private Thread threadUpdate;

        private Dictionary<byte, Action<NetIncomingMessage>> callbacks;

#if DEBUG
        public int Down, Up;
        private int downLast, upLast, statsReset;
#endif

        public event Action OnDisconnected;
        public event Action<NetIncomingMessage> OnUpdateAllPlayers;

        public bool IsConnected => (client != null && client.ConnectionStatus != NetConnectionStatus.Disconnected && client.ConnectionStatus != NetConnectionStatus.Disconnecting);

        public float AverageRoundtripTime => (client != null && client.ServerConnection != null ? client.ServerConnection.AverageRoundtripTime : 0);

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

            threadUpdate = new Thread(OnHandleMessagesThread);
            threadUpdate.IsBackground = true;
            threadUpdate.Start();
        }

        public void Connect(string host, int port)
        {
            Connect(new IPEndPoint(NetUtility.Resolve(host), port));
        }

        public void Connect(IPEndPoint host)
        {
            NetOutgoingMessage message = client.CreateMessage(4);
            message.Write((byte)0); // Flags

            byte major, minor, build;
            App.GetAssemblyVersionNumber(out major, out minor, out build);
            message.Write((byte)major);
            message.Write((byte)minor);
            message.Write((byte)build);

            client.Connect(host, message);
        }

        public void Close()
        {
            if (threadUpdate == null) {
                return;
            }

            threadUpdate = null;

            client.Shutdown("Client is disconnecting");
        }

        private void OnHandleMessagesThread()
        {
            while (threadUpdate != null) {
                client.MessageReceivedEvent.WaitOne();

#if DEBUG
                int nowTime = (int)(NetTime.Now * 1000);
                if (nowTime - statsReset > 1000) {
                    statsReset = nowTime;
                    Down = downLast;
                    Up = upLast;
                    downLast = 0;
                    upLast = 0;
                }
#endif

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
                                
                            if (status == NetConnectionStatus.Disconnected) {
                                OnDisconnected?.Invoke();
                            }

                            break;

                        case NetIncomingMessageType.Data: {
#if DEBUG
                            downLast += msg.LengthBytes;
#endif
                            byte type = msg.ReadByte();

                            if (type == PacketTypes.UpdateAll) {
                                OnUpdateAllPlayers?.Invoke(msg);
                            } else {
                                Action<NetIncomingMessage> callback;
                                if (callbacks.TryGetValue(type, out callback)) {
                                    callback(msg);
                                } else {
                                    Console.WriteLine("        - Unknown packet type (" + type + ")!");
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

            client = null;

            Debug.WriteLine("NetworkHandler: OnHandleMessagesThread exited!");
        }

        #region Messages

        public bool Send<T>(T packet, int capacity, NetDeliveryMethod method, int channel) where T : struct, IClientPacket
        {
            if (client == null) {
                return false;
            }

            NetOutgoingMessage msg = client.CreateMessage(capacity);
            msg.Write((byte)packet.Type);
            packet.Write(msg);
            NetSendResult result = client.SendMessage(msg, method, channel);

#if DEBUG
            upLast += msg.LengthBytes;
#endif
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