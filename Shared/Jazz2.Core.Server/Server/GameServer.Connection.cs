#if MULTIPLAYER

using System;
using System.Collections.Generic;
using Duality;
using Jazz2.Networking;
using Jazz2.Networking.Packets.Server;
using Jazz2.Server.EventArgs;
using Lidgren.Network;

namespace Jazz2.Server
{
    partial class GameServer
    {
        private void OnClientConnected(ClientConnectedEventArgs args)
        {
            if (serverState == ServerState.Unloaded) {
                args.DenyReason = "server unloaded";
                return;
            }

            // Check ban status of endpoint
            if (bannedEndPoints.Contains(args.Message.SenderConnection?.RemoteEndPoint)) {
                args.DenyReason = "banned";
                return;
            }

            if (args.Message.LengthBytes < 4) {
                args.DenyReason = "incompatible version";
#if DEBUG
                Log.Write(LogType.Warning, "Connection of unsupported client (" + args.Message.SenderConnection.RemoteEndPoint + ") was denied");
#endif
                return;
            }

            byte flags = args.Message.ReadByte();
            // ToDo: flags are unused

            byte major = args.Message.ReadByte();
            byte minor = args.Message.ReadByte();
            byte build = args.Message.ReadByte();
            if (major < neededMajor || (major == neededMajor && (minor < neededMinor || (major == neededMajor && build < neededBuild)))) {
                args.DenyReason = "incompatible version";
#if DEBUG
                Log.Write(LogType.Warning, "Connection of outdated client (" + args.Message.SenderConnection.RemoteEndPoint + ") was denied");
#endif
                return;
            }

            byte[] clientIdentifier = args.Message.ReadBytes(16);

            // Check ban status of client identifier
            if (bannedClientIds.Contains(ClientIdentifierToString(clientIdentifier))) {
                args.DenyReason = "banned";
                return;
            }

            if (allowOnlyUniqueClients) {
                lock (sync) {
                    foreach (KeyValuePair<NetConnection, PlayerClient> pair in players) {
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

            // Ensure that player has unique username
            string userName = args.Message.ReadString();
            lock (sync) {
                if (string.IsNullOrWhiteSpace(userName)) {
                    do {
                        userName = GenerateRandomUserName();
                    } while (FindPlayerByUserName(userName) != null);
                } else {
                    userName = userName.Replace(' ', '_');

                    if (FindPlayerByUserName(userName) != null) {
                        int number = 1;
                        while (FindPlayerByUserName(userName + "_" + number) != null) {
                            number++;
                        }

                        userName = userName + "_" + number;
                    }
                }
            }

            PlayerClient player = new PlayerClient {
                Connection = args.Message.SenderConnection,
                ClientIdentifier = clientIdentifier,
                UserName = userName,
                State = PlayerState.NotReady
            };

            lock (sync) {
                players[args.Message.SenderConnection] = player;
            }
        }

        private void OnClientStatusChanged(ClientStatusChangedEventArgs args)
        {
            if (args.Status == NetConnectionStatus.Connected) {
                PlayerClient player;

                lock (sync) {
                    lastPlayerIndex++;

                    if (players.TryGetValue(args.SenderConnection, out player)) {
                        player.Index = lastPlayerIndex;
                        playersByIndex[lastPlayerIndex] = player;
                    }

                    if (serverState == ServerState.LevelReady && playerSpawningEnabled) {
                        // Take a new player another 60 seconds to load level
                        countdown = 60f;
                        countdownNotify = int.MaxValue;
                    }
                }

                Log.Write(LogType.Verbose, "Player #" + player.Index + " (" + player.UserName + " @ " + args.SenderConnection.RemoteEndPoint + ") connected");

                if (currentLevel != null) {
                    Send(new LoadLevel {
                        ServerName = serverName,
                        LevelName = currentLevel,
                        LevelType = currentLevelType,
                        AssignedPlayerIndex = lastPlayerIndex
                    }, 64, args.SenderConnection, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
                }

            } else if (args.Status == NetConnectionStatus.Disconnected) {
                PlayerClient player;

                lock (sync) {
                    byte index = players[args.SenderConnection].Index;

                    if (players.TryGetValue(args.SenderConnection, out player)) {
                        //collisions.RemoveProxy(player.ShadowActor);

                        if (player.ProxyActor != null) {
                            levelHandler.RemovePlayer(player.ProxyActor);
                        }
                    }

                    players.Remove(args.SenderConnection);
                    playersByIndex[index] = null;
                    playerConnections.Remove(args.SenderConnection);

                    foreach (KeyValuePair<NetConnection, PlayerClient> to in players) {
                        if (to.Key == args.SenderConnection) {
                            continue;
                        }

                        Send(new DestroyRemoteActor {
                            Index = index
                        }, 5, to.Key, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
                    }
                }

                Log.Write(LogType.Verbose, "Player #" + player.Index + " (" + player.UserName + " @ " + args.SenderConnection.RemoteEndPoint + ") disconnected");
            }
        }

        private void OnMessageReceived(MessageReceivedEventArgs args)
        {
            if (args.IsUnconnected) {
                if (args.Message.LengthBytes == 1 && args.Message.PeekByte() == SpecialPacketTypes.Ping) {
                    // Fast path for Ping request
                    NetOutgoingMessage m = server.CreateMessage();
                    m.Write(SpecialPacketTypes.Ping);
                    m.Write(server.UniqueIdentifier);
                    server.SendUnconnected(m, args.Message.SenderEndPoint);
                    return;
                }

                string identifier = args.Message.ReadString();
                if (identifier != Token) {
#if DEBUG
                    Log.Write(LogType.Warning, "Request from unsupported client (" + args.Message.SenderConnection?.RemoteEndPoint + ") was denied");
#endif
                    return;
                }

                byte major = args.Message.ReadByte();
                byte minor = args.Message.ReadByte();
                byte build = args.Message.ReadByte();
                if (major < neededMajor || (major == neededMajor && (minor < neededMinor || (major == neededMajor && build < neededBuild)))) {
#if DEBUG
                    Log.Write(LogType.Warning, "Request from outdated client (" + args.Message.SenderConnection?.RemoteEndPoint + ") was denied");
#endif
                    return;
                }
            }

            byte type = args.Message.ReadByte();

            Action<NetIncomingMessage, bool> callback = callbacks[type];
            if (callback != null) {
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
            msg.Write(serverName);

            byte flags = 0;
            // ToDo: Password protected servers
            msg.Write((byte)flags);

            msg.WriteVariableInt32(server.ConnectionsCount);
            msg.WriteVariableInt32(maxPlayers);

            args.Message = msg;
        }

        #region Callbacks
        public void AddCallback<T>(PacketCallback<T> callback) where T : struct, IClientPacket
        {
            byte type = (new T().Type);
#if DEBUG
            if (callbacks[type] != null) {
                throw new InvalidOperationException("Packet callback with this type was already registered");
            }
#endif
            callbacks[type] = (msg, isUnconnected) => ProcessCallback(msg, isUnconnected, callback);
        }

        public void RemoveCallback<T>() where T : struct, IClientPacket
        {
            byte type = (new T().Type);
            callbacks[type] = null;
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

#if DEBUG
            if (msg.LengthBytes > capacity) {
                Log.Write(LogType.Warning, "Packet " + typeof(T).Name + " has underestimated capacity (" + msg.LengthBytes + "/" + capacity + ")");
            }
#endif

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

#if DEBUG
            if (msg.LengthBytes > capacity) {
                Log.Write(LogType.Warning, "Packet " + typeof(T).Name + " has underestimated capacity (" + msg.LengthBytes + "/" + capacity + ")");
            }
#endif

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

        public bool SendToActivePlayers<T>(T packet, int capacity, NetDeliveryMethod method, int channel) where T : struct, IServerPacket
        {
            NetOutgoingMessage msg = server.CreateMessage(capacity);
            msg.Write((byte)packet.Type);
            packet.Write(msg);

#if DEBUG
            if (msg.LengthBytes > capacity) {
                Log.Write(LogType.Warning, "Packet " + typeof(T).Name + " has underestimated capacity (" + msg.LengthBytes + "/" + capacity + ")");
            }
#endif

            if (playerConnections.Count > 0) {
#if NETWORK_DEBUG__
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Debug: ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Send<" + typeof(T).Name + ">  " + msg.LengthBytes + " bytes, " + recipients.Count + " recipients");
#endif
                server.Send(msg, playerConnections, method, channel);
                return true;
            } else {
                return false;
            }
        }

        public bool SendToPlayerByIndex<T>(T packet, int capacity, int playerIndex, NetDeliveryMethod method, int channel) where T : struct, IServerPacket
        {
            PlayerClient player = playersByIndex[playerIndex];
            if (player == null) {
                return false;
            }

            NetOutgoingMessage msg = server.CreateMessage(capacity);
            msg.Write((byte)packet.Type);
            packet.Write(msg);

#if DEBUG
            if (msg.LengthBytes > capacity) {
                Log.Write(LogType.Warning, "Packet " + typeof(T).Name + " has underestimated capacity (" + msg.LengthBytes + "/" + capacity + ")");
            }
#endif

#if NETWORK_DEBUG__
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Debug: ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Send<" + typeof(T).Name + ">  " + msg.LengthBytes + " bytes, " + recipients.Count + " recipients");
#endif
            server.Send(msg, player.Connection, method, channel);
            return true;
        }
        #endregion
    }
}

#endif