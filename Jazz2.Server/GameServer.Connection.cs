#if MULTIPLAYER

using System;
using System.Collections.Generic;
using Jazz2.Networking;
using Jazz2.Networking.Packets;
using Jazz2.Networking.Packets.Server;
using Jazz2.Server.EventArgs;
using Lidgren.Network;

namespace Jazz2.Server
{
    partial class GameServer
    {
        private void OnClientConnected(ClientConnectedEventArgs args)
        {
            if (args.Message.LengthBytes < 4) {
                args.DenyReason = "incompatible version";
                Log.Write(LogType.Error, "Connection of unsupported client (" + args.Message.SenderConnection.RemoteEndPoint + ") was denied!");
                return;
            }

            byte flags = args.Message.ReadByte();

            byte major = args.Message.ReadByte();
            byte minor = args.Message.ReadByte();
            byte build = args.Message.ReadByte();
            if (major < neededMajor || (major == neededMajor && (minor < neededMinor || (major == neededMajor && build < neededBuild)))) {
                args.DenyReason = "incompatible version";
                Log.Write(LogType.Error, "Connection of outdated client (" + args.Message.SenderConnection.RemoteEndPoint + ") was denied!");
                return;
            }

            byte[] clientIdentifier = args.Message.ReadBytes(16);
            if (allowOnlyUniqueClients) {
                lock (sync) {
                    foreach (KeyValuePair<NetConnection, Player> pair in players) {
                        bool isSame = true;
                        for (int i = 0; i < 16; i++) {
                            if (clientIdentifier[i] != pair.Value.ClientIdentifier[i]) {
                                isSame = false;
                                break;
                            }
                        }

                        if (isSame) {
                            args.DenyReason = "already connected";
                            return;
                        }
                    }
                }
            }

            string userName = args.Message.ReadString();

            players[args.Message.SenderConnection] = new Player {
                Connection = args.Message.SenderConnection,
                ClientIdentifier = clientIdentifier,
                UserName = userName,
                State = PlayerState.NotReady
            };
        }

        private void OnClientStatusChanged(ClientStatusChangedEventArgs args)
        {
            if (args.Status == NetConnectionStatus.Connected) {
                lock (sync) {
                    lastPlayerIndex++;
                    players[args.SenderConnection].Index = lastPlayerIndex;
                }

                Log.Write(LogType.Verbose, "Client " + PlayerNameToConsole(players[args.SenderConnection]) + " - " + players[args.SenderConnection].UserName + " (" + args.SenderConnection.RemoteEndPoint + ") connected!");

                if (currentLevel != null) {
                    Send(new LoadLevel {
                        LevelName = currentLevel,
                        LevelType = currentLevelType,
                        AssignedPlayerIndex = lastPlayerIndex
                    }, 64, args.SenderConnection, NetDeliveryMethod.ReliableSequenced, PacketChannels.Main);
                }

            } else if (args.Status == NetConnectionStatus.Disconnected) {
                Log.Write(LogType.Verbose, "Client " + PlayerNameToConsole(players[args.SenderConnection]) + " - " + players[args.SenderConnection].UserName + " (" + args.SenderConnection.RemoteEndPoint + ") disconnected!");

                lock (sync) {
                    byte index = players[args.SenderConnection].Index;

                    Player player;
                    if (players.TryGetValue(args.SenderConnection, out player)) {
                        collisions.RemoveProxy(player);
                    }

                    players.Remove(args.SenderConnection);
                    playerConnections.Remove(args.SenderConnection);

                    foreach (KeyValuePair<NetConnection, Player> to in players) {
                        if (to.Key == args.SenderConnection) {
                            continue;
                        }

                        Send(new DestroyRemotePlayer {
                            Index = index,
                            Reason = 1 // ToDo
                        }, 3, to.Key, NetDeliveryMethod.ReliableSequenced, PacketChannels.Main);
                    }
                }
            }
        }

        private void OnMessageReceived(MessageReceivedEventArgs args)
        {
            if (args.IsUnconnected) {
                if (args.Message.LengthBytes == 1 && args.Message.PeekByte() == PacketTypes.Ping) {
                    // Fast path for Ping request
                    NetOutgoingMessage m = server.CreateMessage();
                    m.Write(PacketTypes.Ping);
                    m.Write(server.UniqueIdentifier);
                    server.SendUnconnected(m, args.Message.SenderEndPoint);
                    return;
                }

                string identifier = args.Message.ReadString();
                if (identifier != Token) {
                    Log.Write(LogType.Error, "Request from unsupported client (" + args.Message.SenderConnection?.RemoteEndPoint + ") was denied!");
                    return;
                }

                byte major = args.Message.ReadByte();
                byte minor = args.Message.ReadByte();
                byte build = args.Message.ReadByte();
                if (major < neededMajor || (major == neededMajor && (minor < neededMinor || (major == neededMajor && build < neededBuild)))) {
                    Log.Write(LogType.Error, "Request from outdated client (" + args.Message.SenderConnection?.RemoteEndPoint + ") was denied!");
                    return;
                }
            }

            byte type = args.Message.ReadByte();

            Action<NetIncomingMessage, bool> callback;
            if (callbacks.TryGetValue(type, out callback)) {
                callback(args.Message, args.IsUnconnected);
            }
        }

        private void OnDiscoveryRequest(DiscoveryRequestEventArgs args)
        {
            NetOutgoingMessage msg = server.CreateMessage(64);

            // Header for unconnected message
            msg.Write(Token);
            msg.Write(neededMajor);
            msg.Write(neededMinor);
            msg.Write(neededBuild);

            msg.Write(server.UniqueIdentifier);
            msg.Write(name);

            byte flags = 0;
            // ToDo: Password protected servers
            msg.Write((byte)flags);

            msg.WriteVariableInt32(server.ConnectionsCount);
            msg.WriteVariableInt32(maxPlayers);

            args.Message = msg;
        }

        #region Callbacks
        public void RegisterCallback<T>(PacketCallback<T> callback) where T : struct, IClientPacket
        {
            byte type = (new T().Type);
            callbacks[type] = (msg, isUnconnected) => ProcessCallback(msg, isUnconnected, callback);
        }

        public void RemoveCallback<T>() where T : struct, IClientPacket
        {
            byte type = (new T().Type);
            callbacks.Remove(type);
        }

        private void ProcessCallback<T>(NetIncomingMessage msg, bool isUnconnected, PacketCallback<T> callback) where T : struct, IClientPacket
        {
            T packet = default(T);
            if (isUnconnected && !packet.SupportsUnconnected) {
#if NETWORK_DEBUG__
                Console.WriteLine("        - Packet<" + typeof(T).Name + "> not allowed for unconnected clients!");
#endif
                return;
            }

#if NETWORK_DEBUG__
            Console.WriteLine("        - Packet<" + typeof(T).Name + ">");
#endif

            packet.SenderConnection = msg.SenderConnection;
            packet.Read(msg);
            callback(ref packet);
        }
        #endregion

        #region Messages
        public bool Send<T>(T packet, int capacity, NetConnection recipient, NetDeliveryMethod method, int channel) where T : struct, IServerPacket
        {
            NetOutgoingMessage msg = server.CreateMessage(capacity);
            msg.Write((byte)packet.Type);
            packet.Write(msg);
            NetSendResult result = server.Send(msg, recipient, method, channel);

#if NETWORK_DEBUG__
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Debug: ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Send<" + typeof(T).Name + ">  " + msg.LengthBytes + " bytes");
#endif
            return (result == NetSendResult.Sent || result == NetSendResult.Queued);
        }

        public bool Send<T>(T packet, int capacity, List<NetConnection> recipients, NetDeliveryMethod method, int channel) where T : struct, IServerPacket
        {
            NetOutgoingMessage msg = server.CreateMessage(capacity);
            msg.Write((byte)packet.Type);
            packet.Write(msg);

            if (recipients.Count > 0) {
#if NETWORK_DEBUG__
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Debug: ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Send<" + typeof(T).Name + ">  " + msg.LengthBytes + " bytes, " + recipients.Count + " recipients");
#endif
                server.Send(msg, recipients, method, channel);
                return true;
            } else {
                return false;
            }
        }
        #endregion
    }
}

#endif