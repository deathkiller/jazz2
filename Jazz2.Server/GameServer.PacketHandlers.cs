#if MULTIPLAYER

using System;
using System.Collections.Generic;
using Duality;
using Jazz2.Actors;
using Jazz2.Networking.Packets;
using Jazz2.Networking.Packets.Client;
using Jazz2.Networking.Packets.Server;
using Lidgren.Network;

namespace Jazz2.Server
{
    partial class GameServer
    {
        private void OnLevelReady(ref LevelReady p)
        {
            Player player;
            if (players.TryGetValue(p.SenderConnection, out player)) {
                if (player.Index != p.Index) {
                    throw new InvalidOperationException();
                }

                lock (sync) {
                    player.PlayerType = PlayerType.Jazz;
                    player.HasLevelLoaded = true;

                    playerConnections.Add(p.SenderConnection);

                    foreach (KeyValuePair<NetConnection, Player> pair in players) {
                        if (pair.Key == p.SenderConnection || !pair.Value.HasLevelLoaded) {
                            continue;
                        }

                        Send(new CreateRemotePlayer {
                            Index = player.Index,
                            Type = player.PlayerType,
                            Pos = player.Pos
                        }, 9, pair.Key, NetDeliveryMethod.ReliableSequenced, PacketChannels.Main);
                    }

                    foreach (KeyValuePair<NetConnection, Player> pair in players) {
                        if (pair.Key == p.SenderConnection || !pair.Value.HasLevelLoaded) {
                            continue;
                        }

                        Send(new CreateRemotePlayer {
                            Index = pair.Value.Index,
                            Type = pair.Value.PlayerType,
                            Pos = pair.Value.Pos
                        }, 9, p.SenderConnection, NetDeliveryMethod.ReliableSequenced, PacketChannels.Main);
                    }
                }
            }
        }

        private void OnUpdateSelf(ref UpdateSelf p)
        {
            Player player;
            if (players.TryGetValue(p.SenderConnection, out player)) {
                if (player.Index != p.Index) {
                    throw new InvalidOperationException();
                }

                if (player.LastUpdateTime > p.UpdateTime) {
                    return;
                }

                player.LastUpdateTime = p.UpdateTime;

                float rtt = p.SenderConnection.AverageRoundtripTime;

                Vector3 pos = p.Pos;
                pos.X += p.Speed.X * rtt * 0.5f;
                pos.Y += p.Speed.Y * rtt * 0.5f;
                player.Pos = pos;

                player.Speed = p.Speed;

                player.AnimState = p.AnimState;
                player.AnimTime = p.AnimTime;
                player.IsFacingLeft = p.IsFacingLeft;

                player.IsFirePressed = p.IsFirePressed;
            }
        }
    }
}

#endif