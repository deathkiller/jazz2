#if MULTIPLAYER

using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using Duality;
using Jazz2.Server.EventArgs;
using Lidgren.Network;

namespace Jazz2.Server
{
    public partial class ServerConnection
    {
        private int port;
        private NetServer server;
        private Thread threadUpdate;
        private IList<IPAddress> publicIpAddresses;
        private string uniqueIdentifier;

        public int ConnectionsCount => server.ConnectionsCount;

        public event Action<ClientConnectedEventArgs> ClientConnected;
        public event Action<ClientStatusChangedEventArgs> ClientStatusChanged;
        public event Action<MessageReceivedEventArgs> MessageReceived;
        public event Action<DiscoveryRequestEventArgs> DiscoveryRequest;

        public IList<IPAddress> LocalIPAddresses
        {
            get
            {
                return NetUtility.GetSelfAddresses();
            }
        }

        public IList<IPAddress> PublicIPAddresses
        {
            get
            {
                return publicIpAddresses;
            }
        }

        public string UniqueIdentifier
        {
            get
            {
                return uniqueIdentifier;
            }
        }

        public ServerConnection(string appId, int port, int maxPlayers, bool enableUPnP)
        {
            if (maxPlayers < 0 || maxPlayers >= byte.MaxValue)
                throw new ArgumentOutOfRangeException("Max. number of players must be smaller than " + byte.MaxValue);

            this.port = port;

            NetPeerConfiguration config = new NetPeerConfiguration(appId);
            config.Port = port;
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            config.EnableMessageType(NetIncomingMessageType.NatIntroductionSuccess);
            config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
            config.EnableUPnP = enableUPnP;

#if NETWORK_DEBUG
            //config.SimulatedMinimumLatency = 50 / 1000f;
            //config.SimulatedRandomLatency = 50 / 1000f;
            //config.SimulatedDuplicatesChance = 0.2f;

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
            config.MaxConnections = maxPlayers;
            server = new NetServer(config);
            server.Start();

            uniqueIdentifier = NetUtility.ToHexString(server.UniqueIdentifier);

            threadUpdate = new Thread(OnHandleMessagesThread);
            threadUpdate.IsBackground = true;
            threadUpdate.Start();

            if (config.EnableUPnP) {
                publicIpAddresses = server.UPnP.GetExternalIP();
                server.UPnP.ForwardPort(port, "Jazz2");
                
            }
        }

        public void Close()
        {
            if (threadUpdate == null) {
                return;
            }

            if (server.Configuration.EnableUPnP) {
                server.UPnP.DeleteForwardingRule(port);
            }

            server.Shutdown("shutting down");

            threadUpdate.Join();
            threadUpdate = null;
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public NetOutgoingMessage CreateMessage()
        {
            return server.CreateMessage();
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public NetOutgoingMessage CreateMessage(int size)
        {
            return server.CreateMessage(size);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public NetSendResult Send(NetOutgoingMessage msg, NetConnection recipient, NetDeliveryMethod method)
        {
            return server.SendMessage(msg, recipient, method);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public NetSendResult Send(NetOutgoingMessage msg, NetConnection recipient, NetDeliveryMethod method, int channel)
        {
            return server.SendMessage(msg, recipient, method, channel);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void Send(NetOutgoingMessage msg, IList<NetConnection> recipient, NetDeliveryMethod method, int channel)
        {
            server.SendMessage(msg, recipient, method, channel);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void SendUnconnected(NetOutgoingMessage msg, IPEndPoint endpoint)
        {
            server.SendUnconnectedMessage(msg, endpoint);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void Introduce(IPEndPoint hostInternal, IPEndPoint hostExternal, IPEndPoint clientInternal, IPEndPoint clientExternal, string token)
        {
            server.Introduce(hostInternal, hostExternal, clientInternal, clientExternal, token);
        }

        private void OnHandleMessagesThread()
        {
            while (server.Status != NetPeerStatus.NotRunning) {
                server.MessageReceivedEvent.WaitOne();

                NetIncomingMessage msg;
                while (server.ReadMessage(out msg)) {

                    switch (msg.MessageType) {
                        case NetIncomingMessageType.StatusChanged: {
                            NetConnectionStatus status = (NetConnectionStatus)msg.ReadByte();
#if NETWORK_DEBUG
                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                            Console.Write("    S ");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine("[" + msg.SenderEndPoint + "] " + status);
#endif
                            ClientStatusChangedEventArgs args = new ClientStatusChangedEventArgs(msg.SenderConnection, status);
                            ClientStatusChanged?.Invoke(args);
                            break;
                        }

                        case NetIncomingMessageType.Data: {
#if NETWORK_DEBUG__
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
#if NETWORK_DEBUG
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write("    R ");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine("Unconnected [" + msg.SenderEndPoint + "] " + msg.LengthBytes + " bytes");
#endif

                            MessageReceivedEventArgs args = new MessageReceivedEventArgs(msg, true);
                            MessageReceived?.Invoke(args);

                            break;
                        }

                        case NetIncomingMessageType.ConnectionApproval: {
                            ClientConnectedEventArgs args = new ClientConnectedEventArgs(msg);
                            ClientConnected?.Invoke(args);

                            if (args.DenyReason == null) {
                                msg.SenderConnection.Approve();
                            } else {
                                msg.SenderConnection.Deny(args.DenyReason);
                            }
                            break;
                        }

                        case NetIncomingMessageType.DiscoveryRequest: {
#if NETWORK_DEBUG
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write("    Q ");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine("[" + msg.SenderEndPoint + "] " + msg.LengthBytes + " bytes");
#endif

                            DiscoveryRequestEventArgs args = new DiscoveryRequestEventArgs();
                            DiscoveryRequest?.Invoke(args);

                            server.SendDiscoveryResponse(args.Message, msg.SenderEndPoint);
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

                    server.Recycle(msg);
                }
            }

            server = null;

#if DEBUG
            Log.Write(LogType.Verbose, "[Dev] Thread OnHandleMessagesThread() exited");
#endif
        }
    }
}

#endif