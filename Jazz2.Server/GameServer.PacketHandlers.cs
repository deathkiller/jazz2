#if MULTIPLAYER

using System;
using System.Collections.Generic;
using Duality;
using Jazz2.Actors;
using Jazz2.Game;
using Jazz2.Networking;
using Jazz2.Networking.Packets.Client;
using Jazz2.Networking.Packets.Server;
using Lidgren.Network;

namespace Jazz2.Server
{
    partial class GameServer
    {
        private void RegisterPacketCallbacks()
        {
            AddCallback<LevelReady>(OnLevelReady);
            AddCallback<UpdateSelf>(OnUpdateSelf);

            AddCallback<SelfDied>(OnSelfDied);
            AddCallback<RemotePlayerHit>(OnRemotePlayerHit);

            //RegisterCallback<CreateRemotableActor>(OnCreateRemotableActor);
            //RegisterCallback<UpdateRemotableActor>(OnUpdateRemotableActor);
            //RegisterCallback<DestroyRemotableActor>(OnDestroyRemotableActor);
        }

        /// <summary>
        /// Player finished loading of a level and is ready to play
        /// </summary>
        private void OnLevelReady(ref LevelReady p)
        {
            PlayerClient player = playersByIndex[p.Index];
            if (player == null) {
                return;
            }
            if (player.Connection != p.SenderConnection) {
                throw new InvalidOperationException();
            }

            lock (sync) {
                // Add connection to list, so it will start to receive gameplay events from server
                playerConnections.Add(p.SenderConnection);

                if (!playerSpawningEnabled) {
                    // If spawning is not enabled, postpone spawning of the player,
                    // but send command to create all already spawned players
                    player.State = PlayerState.HasLevelLoaded;

                    foreach (KeyValuePair<NetConnection, PlayerClient> pair in players) {
                        if (pair.Key == p.SenderConnection) {
                            continue;
                        }

                        if (pair.Value.State == PlayerState.Spawned && pair.Value.ProxyActor != null) {
                            Send(new CreateRemotePlayer {
                                Index = pair.Value.Index,
                                Type = pair.Value.ProxyActor.PlayerType,
                                Pos = pair.Value.ProxyActor.Transform.Pos
                            }, 9, p.SenderConnection, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
                        }
                    }
                } else {
                    Vector3 pos = new Vector3(levelHandler.EventMap.GetSpawnPositionForMultiplayer(), LevelHandler.PlayerZ);

                    if (player.ProxyActor == null) {
                        player.ProxyActor = new Player();
                        player.ProxyActor.OnActivated(new ActorActivationDetails {
                            LevelHandler = levelHandler,
                            Pos = pos,
                            Params = new[] { (ushort)MathF.Rnd.OneOf(new[] { PlayerType.Jazz, PlayerType.Spaz, PlayerType.Lori }), (ushort)/*p.Index*/0 }
                        });
                        levelHandler.AddPlayer(player.ProxyActor);
                    } else {
                        player.ProxyActor.Transform.Pos = pos;
                    }

                    player.State = PlayerState.Spawned;

                    Send(new CreateControllablePlayer {
                        Index = player.Index,
                        Type = player.ProxyActor.PlayerType,
                        Pos = pos,
                        Health = playerHealth
                    }, 10, p.SenderConnection, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);

                    // Send command to create all already spawned players
                    List<NetConnection> playersWithLoadedLevel = new List<NetConnection>();
                    foreach (KeyValuePair<NetConnection, PlayerClient> pair in players) {
                        if (pair.Key == p.SenderConnection) {
                            continue;
                        }

                        if (pair.Value.State == PlayerState.HasLevelLoaded || pair.Value.State == PlayerState.Spawned || pair.Value.State == PlayerState.Dead) {
                            playersWithLoadedLevel.Add(pair.Key);

                            if (pair.Value.State == PlayerState.Spawned) {
                                Send(new CreateRemotePlayer {
                                    Index = pair.Value.Index,
                                    Type = pair.Value.ProxyActor.PlayerType,
                                    Pos = pair.Value.ProxyActor.Transform.Pos
                                }, 9, p.SenderConnection, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
                            }
                        }
                    }

                    // Send command to all active players to create this new player
                    Send(new CreateRemotePlayer {
                        Index = player.Index,
                        Type = player.ProxyActor.PlayerType,
                        Pos = pos
                    }, 9, playersWithLoadedLevel, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
                }

                // Send command to create all remotable actors
                /*foreach (KeyValuePair<int, RemotableActor> pair in remotableActors) {
                    RemotableActor remotableActor = pair.Value;

                    Send(new CreateRemoteObject {
                        Index = remotableActor.Index,
                        EventType = remotableActor.EventType,
                        EventParams = remotableActor.EventParams,
                        Pos = remotableActor.Pos,
                    }, 35, p.SenderConnection, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
                }*/

                levelHandler.SendAllSpawnedActors(p.SenderConnection);
            }
        }

        /// <summary>
        /// Player sent its updated state and position
        /// </summary>
        private void OnUpdateSelf(ref UpdateSelf p)
        {
            PlayerClient player = playersByIndex[p.Index];
            if (player == null) {
                return;
            }
            if (player.Connection != p.SenderConnection) {
                throw new InvalidOperationException();
            }

            if (player.LastUpdateTime > p.UpdateTime) {
                return;
            }

            player.LastUpdateTime = p.UpdateTime;

            Player proxyActor = player.ProxyActor;
            if (proxyActor == null) {
                return;
            }

            proxyActor.Transform.Pos = p.Pos;

            // ToDo
            /*player.AnimTime = p.AnimTime;
            player.Controllable = p.Controllable;
            player.IsFirePressed = p.IsFirePressed;*/

            proxyActor.SyncWithClient(p.AnimState, p.IsFacingLeft);
        }

        /// <summary>
        /// Player died
        /// </summary>
        private void OnSelfDied(ref SelfDied p)
        {
            PlayerClient player = playersByIndex[p.Index];
            if (player == null) {
                return;
            }
            if (player.Connection != p.SenderConnection) {
                throw new InvalidOperationException();
            }

            player.State = PlayerState.Dead;
            player.StatsDeaths++;

            SendToActivePlayers(new RemotePlayerDied {
                Index = player.Index
            }, 2, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
        }

        /// <summary>
        /// Player hit (with weapon) another player
        /// </summary>
        private void OnRemotePlayerHit(ref RemotePlayerHit p)
        {
            // ToDo: This packet should be verified by server
            PlayerClient player = playersByIndex[p.Index];
            if (player == null) {
                return;
            }

            if (players.TryGetValue(p.SenderConnection, out PlayerClient attacker)) {
                return;
            }

            attacker.StatsHits++;

            SendToActivePlayers(new DecreasePlayerHealth {
                Index = p.Index,
                Amount = p.Damage
            }, 3, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
        }

        /// <summary>
        /// Player requested to create remotable actor owned by the player
        /// </summary>
        /*private void OnCreateRemotableActor(ref CreateRemotableActor p)
        {
            if (p.EventType == EventType.Empty) {
                return;
            }

            int index = p.Index;

            Player player;
            RemotableActor remotableActor = new RemotableActor {
                Index = index,
                EventType = p.EventType,
                EventParams = p.EventParams,
                Pos = p.Pos
            };

            lock (sync) {
                if (!players.TryGetValue(p.SenderConnection, out player)) {
                    return;
                }

                // Subindex corresponds to player index of owner
                if (player.Index != (index & 0xff)) {
                    return;
                }

                remotableActor.Owner = player;

                remotableActors[index] = remotableActor;

                remotableActor.AABB = new AABB(remotableActor.Pos.Xy, 30, 30);
                collisions.AddProxy(remotableActor);
            }

            Send(new CreateRemoteObject {
                Index = remotableActor.Index,
                EventType = remotableActor.EventType,
                EventParams = remotableActor.EventParams,
                Pos = remotableActor.Pos,
            }, 35, playerConnections, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
        }*/

        /// <summary>
        /// Player updated state and position of owned remotable actor
        /// </summary>
        /*private void OnUpdateRemotableActor(ref UpdateRemotableActor p)
        {
            int index = p.Index;

            Player player;

            lock (sync) {
                if (!players.TryGetValue(p.SenderConnection, out player)) {
                    return;
                }

                // Subindex corresponds to player index of owner
                if (player.Index != (index & 0xff)) {
                    return;
                }

                RemotableActor remotableActor;
                if (!remotableActors.TryGetValue(index, out remotableActor)) {
                    return;
                }

                if (remotableActor.LastUpdateTime > p.UpdateTime) {
                    return;
                }

                remotableActor.LastUpdateTime = p.UpdateTime;

                float rtt = p.SenderConnection.AverageRoundtripTime;

                Vector3 pos = p.Pos;
                pos.X += p.Speed.X * rtt;
                pos.Y += p.Speed.Y * rtt;
                remotableActor.Pos = pos;

                remotableActor.Speed = p.Speed;

                remotableActor.AnimState = p.AnimState;
                remotableActor.AnimTime = p.AnimTime;
                remotableActor.IsFacingLeft = p.IsFacingLeft;

                //remotableObject.AABB = new AABB(remotableObject.Pos.Xy, 30, 30);
                //collisions.AddProxy(remotableObject);
            }
        }*/

        /// <summary>
        /// Player destroyed owned remotable actor
        /// </summary>
        /*private void OnDestroyRemotableActor(ref DestroyRemotableActor p)
        {
            int index = p.Index;

            lock (sync) {
                Player player;
                if (!players.TryGetValue(p.SenderConnection, out player)) {
                    return;
                }

                // Subindex corresponds to player index of owner
                if (player.Index != (index & 0xff)) {
                    return;
                }

                if (!remotableActors.Remove(index)) {
                    return;
                }
            }

            Send(new DestroyRemoteObject {
                Index = index
            }, 5, playerConnections, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
        }*/
    }
}

#endif