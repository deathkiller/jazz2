#if MULTIPLAYER

using System;
using System.Collections.Generic;
using Jazz2.Networking;
using Jazz2.Networking.Components;
using Jazz2.Networking.Packets;
using Jazz2.Networking.Packets.Server;
using Jazz2.Server.EventArgs;
using Lidgren.Network;

namespace Jazz2.Server
{
    partial class App
    {
        private static void OnClientConnected(ClientConnectedEventArgs args)
        {
            if (args.Message.LengthBytes < 4) {
                Console.WriteLine("        - Corrupted OnClientConnected message!");
                return;
            }

            byte flags = args.Message.ReadByte();

            byte major = args.Message.ReadByte();
            byte minor = args.Message.ReadByte();
            byte build = args.Message.ReadByte();
            if (major < neededMajor || (major == neededMajor && (minor < neededMinor || (major == neededMajor && build < neededBuild)))) {
                Console.WriteLine("        - Incompatible version!");
                return;
            }

            args.Allow = true;

            players[args.Message.SenderConnection] = new Player {
                IsReady = false
            };
        }

        private static void OnClientStatusChanged(ClientStatusChangedEventArgs args)
        {
            if (args.Status == NetConnectionStatus.Connected) {
                lastPlayerIndex++;

                players[args.SenderConnection].Index = lastPlayerIndex;

                Send(new LoadLevel {
                    LevelName = currentLevel,
                    LevelType = MultiplayerLevelType.Battle,
                    AssignedPlayerIndex = lastPlayerIndex
                }, 64, args.SenderConnection, NetDeliveryMethod.ReliableSequenced, PacketChannels.Main);
            } else if (args.Status == NetConnectionStatus.Disconnected) {
                int index = players[args.SenderConnection].Index;

                players.Remove(args.SenderConnection);
                playerConnections.Remove(args.SenderConnection);

                foreach (var to in players) {
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

        private static void OnMessageReceived(MessageReceivedEventArgs args)
        {
            if (args.IsUnconnected) {
                if (args.Message.LengthBytes == 1 && args.Message.ReadByte() == PacketTypes.Ping) {
                    // It's probably only ping request
                    NetOutgoingMessage m = server.CreateMessage();
                    m.Write(PacketTypes.Ping);
                    server.SendUnconnected(m, args.Message.SenderEndPoint);
                    return;
                }

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
#if DEBUG__
                Console.WriteLine("        - Packet<" + typeof(T).Name + "> not allowed for unconnected clients!");
#endif
                return;
            }

#if DEBUG__
            Console.WriteLine("        - Packet<" + typeof(T).Name + ">");
#endif

            packet.SenderConnection = msg.SenderConnection;
            packet.Read(msg);
            callback(ref packet);
        }
        #endregion

        #region Messages
        public static bool Send<T>(T packet, int capacity, NetConnection recipient, NetDeliveryMethod method, int channel) where T : struct, IServerPacket
        {
            NetOutgoingMessage msg = server.CreateMessage(capacity);
            msg.Write((byte)packet.Type);
            packet.Write(msg);
            NetSendResult result = server.Send(msg, recipient, method, channel);

#if DEBUG__
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Debug: ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Send<" + typeof(T).Name + ">  " + msg.LengthBytes + " bytes");
#endif
            return (result == NetSendResult.Sent || result == NetSendResult.Queued);
        }

        public static bool Send<T>(T packet, int capacity, List<NetConnection> recipients, NetDeliveryMethod method, int channel) where T : struct, IServerPacket
        {
            NetOutgoingMessage msg = server.CreateMessage(capacity);
            msg.Write((byte)packet.Type);
            packet.Write(msg);

            if (recipients.Count > 0) {
#if DEBUG__
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