#if MULTIPLAYER && !SERVER

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Duality;
using Jazz2.Networking;
using Lidgren.Network;

namespace Jazz2.Game
{
    public class GameClient
    {
        private byte[] clientIdentifier;
        private string userName;
        private NetClient client;
        private Thread threadUpdate;

        private Dictionary<byte, Action<NetIncomingMessage>> callbacks;

#if DEBUG
        public int DownloadPacketBytes, UploadPacketBytes;
        private int downloadPacketBytesLast, uploadPacketBytesLast, statsLastTime;
#endif

        public event Action OnDisconnected;
        public event Action<NetIncomingMessage> OnUpdateAllActors;

        public bool IsConnected => (client != null && client.ConnectionStatus != NetConnectionStatus.Disconnected && client.ConnectionStatus != NetConnectionStatus.Disconnecting);

        public float AverageRoundtripTime => (client != null && client.ServerConnection != null ? client.ServerConnection.AverageRoundtripTime : 0);

        public GameClient(string appId, byte[] clientIdentifier, string userName)
        {
            if (clientIdentifier == null || clientIdentifier.Length != 16) {
                throw new ArgumentException("Client identifier must be 16 bytes long");
            }

            this.clientIdentifier = clientIdentifier;
            this.userName = userName;

            callbacks = new Dictionary<byte, Action<NetIncomingMessage>>();

            NetPeerConfiguration config = new NetPeerConfiguration(appId);
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

            threadUpdate = new Thread(OnHandleMessagesThread);
            threadUpdate.IsBackground = true;
            threadUpdate.Start();
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

            for (int i = 0; i < 16; i++) {
                message.Write((byte)clientIdentifier[i]);
            }

            message.Write(userName);

            client.Connect(host, message);
        }

        public void Close()
        {
            if (threadUpdate == null) {
                return;
            }

            threadUpdate = null;

            client.Shutdown("disconnecting");
        }

        private void OnHandleMessagesThread()
        {
            while (threadUpdate != null) {
                client.MessageReceivedEvent.WaitOne();

#if DEBUG
                int nowTime = (int)(NetTime.Now * 1000);
                if (nowTime - statsLastTime > 1000) {
                    statsLastTime = nowTime;
                    DownloadPacketBytes = downloadPacketBytesLast;
                    UploadPacketBytes = uploadPacketBytesLast;
                    downloadPacketBytesLast = 0;
                    uploadPacketBytesLast = 0;
                }
#endif

                NetIncomingMessage msg;
                while (client.ReadMessage(out msg)) {
                    switch (msg.MessageType) {
                        case NetIncomingMessageType.StatusChanged:
                            NetConnectionStatus status = (NetConnectionStatus)msg.ReadByte();
#if NETWORK_DEBUG
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
                            downloadPacketBytesLast += msg.LengthBytes;
#endif
                            byte type = msg.ReadByte();

                            if (type == SpecialPacketTypes.UpdateAllActors) {
                                OnUpdateAllActors?.Invoke(msg);
                            } else {
                                Action<NetIncomingMessage> callback;
                                if (callbacks.TryGetValue(type, out callback)) {
                                    callback(msg);
                                } else {
#if DEBUG
                                    Log.Write(LogType.Info, "[Dev] Unknown packet type (0x" + type.ToString("X") + ") received");
#endif
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

#if DEBUG
            Log.Write(LogType.Verbose, "[Dev] Thread OnHandleMessagesThread() exited");
#endif
        }

        #region Messages

        public bool SendToServer<T>(T packet, int capacity, NetDeliveryMethod method, int channel) where T : struct, IClientPacket
        {
            if (client == null) {
                return false;
            }

            NetOutgoingMessage msg = client.CreateMessage(capacity);
            msg.Write((byte)packet.Type);
            packet.Write(msg);

#if DEBUG
            if (msg.LengthBytes > capacity) {
                Log.Write(LogType.Warning, "[Dev] Packet " + typeof(T).Name + " has underestimated capacity (" + msg.LengthBytes + "/" + capacity + ")");
            }
#endif

            NetSendResult result = client.SendMessage(msg, method, channel);

#if DEBUG
            uploadPacketBytesLast += msg.LengthBytes;
#endif
#if NETWORK_DEBUG__
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Debug: ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Send<" + typeof(T).Name + ">  " + msg.LengthBytes + " bytes");
#endif
            return (result == NetSendResult.Sent || result == NetSendResult.Queued);
        }
        #endregion

        #region Callbacks
        public void AddCallback<T>(PacketCallback<T> callback) where T : struct, IServerPacket
        {
            byte type = (new T().Type);
#if DEBUG
            if (callbacks.ContainsKey(type)) {
                throw new InvalidOperationException("Packet callback with this type was already registered");
            }
#endif
            callbacks[type] = (msg) => ProcessCallback(msg, callback);
        }

        public void RemoveCallback<T>() where T : struct, IServerPacket
        {
            byte type = (new T().Type);
            callbacks.Remove(type);
        }

        private static void ProcessCallback<T>(NetIncomingMessage msg, PacketCallback<T> callback) where T : struct, IServerPacket
        {
            T packet = default(T);
#if NETWORK_DEBUG__
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