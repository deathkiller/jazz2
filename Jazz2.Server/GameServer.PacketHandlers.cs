#if MULTIPLAYER

using System;
using System.Collections.Generic;
using Duality;
using Jazz2.Actors;
using Jazz2.Game;
using Jazz2.Game.Collisions;
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

                    playerConnections.Add(p.SenderConnection);

                    if (!enablePlayerSpawning) {
                        player.State = PlayerState.HasLevelLoaded;

                        foreach (KeyValuePair<NetConnection, Player> pair in players) {
                            if (pair.Key == p.SenderConnection) {
                                continue;
                            }

                            if (pair.Value.State == PlayerState.Spawned) {
                                Send(new CreateRemotePlayer {
                                    Index = pair.Value.Index,
                                    Type = pair.Value.PlayerType,
                                    Pos = pair.Value.Pos
                                }, 9, p.SenderConnection, NetDeliveryMethod.ReliableUnordered, PacketChannels.Main);
                            }
                        }

                        return;
                    }

                    // ToDo: Set character requested by the player
                    player.PlayerType = MathF.Rnd.OneOf(new[] { PlayerType.Jazz, PlayerType.Spaz, PlayerType.Lori });

                    player.Pos = new Vector3(eventMap.GetRandomSpawnPosition(), LevelHandler.PlayerZ);
                    player.State = PlayerState.Spawned;

                    player.AABB = new AABB(player.Pos.Xy, 20, 30);
                    collisions.AddProxy(player);

                    Send(new CreateControllablePlayer {
                        Index = player.Index,
                        Type = player.PlayerType,
                        Pos = player.Pos
                    }, 9, p.SenderConnection, NetDeliveryMethod.ReliableUnordered, PacketChannels.Main);

                    List<NetConnection> playersWithLoadedLevel = new List<NetConnection>();
                    foreach (KeyValuePair<NetConnection, Player> pair in players) {
                        if (pair.Key == p.SenderConnection) {
                            continue;
                        }

                        if (pair.Value.State == PlayerState.HasLevelLoaded || pair.Value.State == PlayerState.Spawned || pair.Value.State == PlayerState.Dead) {
                            playersWithLoadedLevel.Add(pair.Key);

                            if (pair.Value.State == PlayerState.Spawned) {
                                Send(new CreateRemotePlayer {
                                    Index = pair.Value.Index,
                                    Type = pair.Value.PlayerType,
                                    Pos = pair.Value.Pos
                                }, 9, p.SenderConnection, NetDeliveryMethod.ReliableUnordered, PacketChannels.Main);
                            }
                        }
                    }

                    Send(new CreateRemotePlayer {
                        Index = player.Index,
                        Type = player.PlayerType,
                        Pos = player.Pos
                    }, 9, playersWithLoadedLevel, NetDeliveryMethod.ReliableUnordered, PacketChannels.Main);
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
                pos.X += p.Speed.X * rtt;
                pos.Y += p.Speed.Y * rtt;
                player.Pos = pos;

                player.Speed = p.Speed;

                player.AnimState = p.AnimState;
                player.AnimTime = p.AnimTime;
                player.IsFacingLeft = p.IsFacingLeft;

                player.Controllable = p.Controllable;
                player.IsFirePressed = p.IsFirePressed;
            }
        }

        private void OnSelfDied(ref SelfDied p)
        {
            Player player;

            lock (sync) {
                if (!players.TryGetValue(p.SenderConnection, out player)) {
                    return;
                }

                player.State = PlayerState.Dead;
            }

            Send(new RemotePlayerDied {
                Index = player.Index
            }, 2, playerConnections, NetDeliveryMethod.ReliableUnordered, PacketChannels.Main);
        }
    }
}

#endif