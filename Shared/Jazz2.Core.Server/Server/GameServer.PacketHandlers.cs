#if MULTIPLAYER

using System;
using System.Collections.Generic;
using Duality;
using Duality.Async;
using Jazz2.Actors;
using Jazz2.Game;
using Jazz2.Game.Structs;
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
            AddCallback<LevelReady>(OnPacketLevelReady);
            AddCallback<PlayerUpdate>(OnPacketPlayerUpdate);
            AddCallback<PlayerRefreshAnimation>(OnPacketPlayerRefreshAnimation);
            AddCallback<PlayerFireWeapon>(OnPacketPlayerFireWeapon);
        }

        /// <summary>
        /// Player finished loading of a level and is ready to play
        /// </summary>
        private void OnPacketLevelReady(ref LevelReady p)
        {
            PlayerClient player = playersByIndex[p.Index];
            if (player == null) {
                return;
            }
            if (player.Connection != p.SenderConnection) {
                throw new InvalidOperationException();
            }

            // TODO: Allow player type change
            player.PlayerType = p.PlayerType;

            lock (sync) {
                if (player.State >= PlayerState.HasLevelLoaded) {
                    // Player has already loaded level, he might want to only change player type
                    return;
                }

                // Add connection to list, so it will start to receive gameplay events from server
                if (!playerConnections.Contains(p.SenderConnection)) {
                    playerConnections.Add(p.SenderConnection);
                }

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
                            switch (pair.Value.PlayerType) {
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
                            Params = new[] { (ushort)player.PlayerType, (ushort)0 }
                        });
                        levelHandler.AddPlayer(player.ProxyActor);
                    } else {
                        player.ProxyActor.Transform.Pos = pos;
                    }

                    player.State = PlayerState.Spawned;

                    Send(new CreateControllablePlayer {
                        Index = player.Index,
                        Type = player.PlayerType,
                        Pos = pos,
                        Health = playerHealth,
                        Controllable = levelStarted
                    }, 11, p.SenderConnection, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);

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
                                switch (pair.Value.PlayerType) {
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

                    switch (player.PlayerType) {
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

                if (!levelStarted) {
                    bool allLoaded = true;
                    if (playerSpawningEnabled) {
                        // If spawning is not enabled, don't wait anymore, because another player cannot be spawned anyway
                        foreach (var pair in players) {
                            if (pair.Value.State == PlayerState.NotReady) {
                                allLoaded = false;
                                break;
                            }
                        }
                    }

                    if (allLoaded) {
                        // All players are ready to player, start game in 15 seconds
                        countdown = 15f;
                        countdownNotify = int.MaxValue;
                    }
                }

                if (currentLevelType == MultiplayerLevelType.Race) {
                    SendToActivePlayers(new PlayerSetLaps {
                        Index = (byte)0,
                        Laps = 0,
                        LapsTotal = raceTotalLaps
                    }, 4, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);
                } else if (CurrentLevelType == MultiplayerLevelType.TreasureHunt) {
                    SendToActivePlayers(new PlayerSetLaps {
                        Index = (byte)0,
                        Laps = 0,
                        LapsTotal = treasureHuntTotalGems
                    }, 4, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);
                }
            }

#if DEBUG
            Log.Write(LogType.Verbose, "Player #" + player.Index + " is ready to play");
#endif
        }

        /// <summary>
        /// Player sent its updated state and position
        /// </summary>
        private void OnPacketPlayerUpdate(ref PlayerUpdate p)
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
            proxyActor.SyncWithClient(p.Speed, p.CurrentSpecialMove, p.IsVisible, p.IsFacingLeft, p.IsActivelyPushing);
        }

        private void OnPacketPlayerRefreshAnimation(ref PlayerRefreshAnimation p)
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

        private void OnPacketPlayerFireWeapon(ref PlayerFireWeapon p)
        {
            PlayerClient player = playersByIndex[p.Index];
            if (player == null) {
                return;
            }
            if (player.Connection != p.SenderConnection) {
                throw new InvalidOperationException();
            }

            Vector3 initialPos = p.InitialPos;
            Vector3 gunspotPos = p.GunspotPos;
            float angle = p.Angle;
            WeaponType weaponType = p.WeaponType;

            Player proxyActor = player.ProxyActor;

            Await.NextUpdate().OnCompleted(() => {
                if (proxyActor == null) {
                    return;
                }

                proxyActor.LastInitialPos = initialPos;
                proxyActor.LastGunspotPos = gunspotPos;
                proxyActor.LastAngle = angle;

                proxyActor.FireWeapon(weaponType);
            });
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