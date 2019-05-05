#if MULTIPLAYER

using System;
using System.Collections.Generic;
using Duality;
using Jazz2.Actors;
using Jazz2.Game;
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
                    player.PlayerType = MathF.Rnd.OneOf(new[] { PlayerType.Jazz, PlayerType.Spaz, PlayerType.Lori, PlayerType.Frog });

                    // ToDo: Spawn player later
                    // ToDo: Set player position from event map
                    player.Pos = new Vector3(eventMap.GetRandomSpawnPosition(), LevelHandler.PlayerZ);
                    

                    player.State = PlayerState.Spawned;

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

                        if (pair.Value.State == PlayerState.HasLevelLoaded || pair.Value.State == PlayerState.Spawned) {
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
                pos.X += p.Speed.X * rtt * 0.5f;
                pos.Y += p.Speed.Y * rtt * 0.5f;
                player.Pos = pos;

                player.Speed = p.Speed;

                player.AnimState = p.AnimState;
                player.AnimTime = p.AnimTime;
                player.IsFacingLeft = p.IsFacingLeft;

                player.Controllable = p.Controllable;
                player.IsFirePressed = p.IsFirePressed;
            }
        }
    }
}

#endif