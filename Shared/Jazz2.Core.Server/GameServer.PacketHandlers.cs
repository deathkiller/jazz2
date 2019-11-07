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
            AddCallback<PlayerUpdate>(OnPlayerUpdate);
            AddCallback<PlayerRefreshAnimation>(OnPlayerRefreshAnimation);

            AddCallback<PlayerDied>(OnPlayerDied);

            AddCallback<PlayerFireWeapon>(OnPlayerFireWeapon);
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
                            string metadataPath;
                            switch (pair.Value.ProxyActor.PlayerType) {
                                default:
                                case PlayerType.Jazz:
                                    metadataPath = "Interactive/PlayerJazz";
                                    break;
                                case PlayerType.Spaz:
                                    metadataPath = "Interactive/PlayerSpaz";
                                    break;
                                case PlayerType.Lori:
                                    metadataPath = "Interactive/PlayerLori";
                                    break;
                                case PlayerType.Frog:
                                    metadataPath = "Interactive/PlayerFrog";
                                    break;
                            }

                            Send(new CreateRemoteActor {
                                Index = pair.Value.Index,
                                CollisionFlags = CollisionFlags.ApplyGravitation,
                                MetadataPath = metadataPath,
                                Pos = pair.Value.ProxyActor.Transform.Pos
                            }, 64, p.SenderConnection, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
                        }
                    }
                } else {
                    Vector3 pos = new Vector3(levelHandler.EventMap.GetSpawnPositionForMultiplayer(), LevelHandler.PlayerZ);

                    if (player.ProxyActor == null) {
                        player.ProxyActor = new Player();
                        player.ProxyActor.Index = player.Index;
                        player.ProxyActor.OnActivated(new ActorActivationDetails {
                            LevelHandler = levelHandler,
                            Pos = pos,
                            Params = new[] { (ushort)MathF.Rnd.OneOf(new[] { PlayerType.Jazz, PlayerType.Spaz, PlayerType.Lori }), (ushort)0 }
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
                    string metadataPath;
                    List<NetConnection> playersWithLoadedLevel = new List<NetConnection>();
                    foreach (KeyValuePair<NetConnection, PlayerClient> pair in players) {
                        if (pair.Key == p.SenderConnection) {
                            continue;
                        }

                        if (pair.Value.State == PlayerState.HasLevelLoaded || pair.Value.State == PlayerState.Spawned || pair.Value.State == PlayerState.Dead) {
                            playersWithLoadedLevel.Add(pair.Key);

                            if (pair.Value.State == PlayerState.Spawned) {
                                switch (pair.Value.ProxyActor.PlayerType) {
                                    default:
                                    case PlayerType.Jazz:
                                        metadataPath = "Interactive/PlayerJazz";
                                        break;
                                    case PlayerType.Spaz:
                                        metadataPath = "Interactive/PlayerSpaz";
                                        break;
                                    case PlayerType.Lori:
                                        metadataPath = "Interactive/PlayerLori";
                                        break;
                                    case PlayerType.Frog:
                                        metadataPath = "Interactive/PlayerFrog";
                                        break;
                                }

                                Send(new CreateRemoteActor {
                                    Index = pair.Value.Index,
                                    CollisionFlags = CollisionFlags.ApplyGravitation,
                                    MetadataPath = metadataPath,
                                    Pos = pair.Value.ProxyActor.Transform.Pos
                                }, 64, p.SenderConnection, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
                            }
                        }
                    }

                    switch (player.ProxyActor.PlayerType) {
                        default:
                        case PlayerType.Jazz:
                            metadataPath = "Interactive/PlayerJazz";
                            break;
                        case PlayerType.Spaz:
                            metadataPath = "Interactive/PlayerSpaz";
                            break;
                        case PlayerType.Lori:
                            metadataPath = "Interactive/PlayerLori";
                            break;
                        case PlayerType.Frog:
                            metadataPath = "Interactive/PlayerFrog";
                            break;
                    }

                    // Send command to all active players to create this new player
                    Send(new CreateRemoteActor {
                        Index = player.Index,
                        CollisionFlags = CollisionFlags.ApplyGravitation,
                        MetadataPath = metadataPath,
                        Pos = pos
                    }, 64, playersWithLoadedLevel, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
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

#if DEBUG
            Log.Write(LogType.Verbose, "[Dev] Player #" + player.Index + " is ready to play");
#endif
        }

        /// <summary>
        /// Player sent its updated state and position
        /// </summary>
        private void OnPlayerUpdate(ref PlayerUpdate p)
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
            proxyActor.SyncWithClient(p.CurrentSpecialMove, p.IsFacingLeft);
        }

        private void OnPlayerRefreshAnimation(ref PlayerRefreshAnimation p)
        {
            PlayerClient player = playersByIndex[p.Index];
            if (player == null) {
                return;
            }
            if (player.Connection != p.SenderConnection) {
                throw new InvalidOperationException();
            }

            Player proxyActor = player.ProxyActor;
            if (proxyActor == null) {
                return;
            }

            proxyActor.OnRefreshActorAnimation(p.Identifier);

            SendToActivePlayers(new RefreshActorAnimation {
                Index = player.Index,
                Identifier = p.Identifier
            }, 48, NetDeliveryMethod.Unreliable, PacketChannels.UnorderedUpdates);
        }

        /// <summary>
        /// Player died
        /// </summary>
        private void OnPlayerDied(ref PlayerDied p)
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

            /*SendToActivePlayers(new RemotePlayerDied {
                Index = player.Index
            }, 2, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);*/

#if DEBUG
            Log.Write(LogType.Verbose, "[Dev] Player #" + player.Index + " died");
#endif
        }

        private void OnPlayerFireWeapon(ref PlayerFireWeapon p)
        {
            PlayerClient player = playersByIndex[p.Index];
            if (player == null) {
                return;
            }
            if (player.Connection != p.SenderConnection) {
                throw new InvalidOperationException();
            }

            Player proxyActor = player.ProxyActor;
            if (proxyActor == null) {
                return;
            }

            proxyActor.FireWeapon(p.WeaponType);
        }

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