#if MULTIPLAYER

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using Duality;
using Duality.Async;
using Duality.IO;
using Duality.Resources;
using Jazz2.Actors;
using Jazz2.Game;
using Jazz2.Networking;
using Jazz2.Networking.Packets.Server;
using Jazz2.Storage.Content;
using Lidgren.Network;

namespace Jazz2.Server
{
    partial class GameServer
    {
        public enum PlayerState
        {
            Unknown,

            NotReady,
            HasLevelLoaded,
            Spawned,
            Dead
        }

        public class PlayerClient
        {
            public byte Index;
            public NetConnection Connection;
            public byte[] ClientIdentifier;
            public string UserName;
            public long LastUpdateTime;
            public PlayerState State;

            public int StatsDeaths;
            public int StatsKills;
            public int StatsHits;
            public int LastHitPlayerIndex;

            public int CurrentLap;
            public double CurrentLapTime;

            public Player ProxyActor;
        }

        private LevelHandler levelHandler;

        private string currentLevel;
        private string currentLevelFriendlyName;
        private MultiplayerLevelType currentLevelType;
        private double startTime;

        private Dictionary<NetConnection, PlayerClient> players;
        public PlayerClient[] playersByIndex;
        private List<NetConnection> playerConnections;
        private byte lastPlayerIndex;
        private bool playerSpawningEnabled = true;
        private byte playerHealth = 5;

        private int lastGameLoadMs;

        public bool IsPlayerSpawningEnabled
        {
            get
            {
                return playerSpawningEnabled;
            }
            set
            {
                if (playerSpawningEnabled == value) {
                    return;
                }

                playerSpawningEnabled = value;

                if (playerSpawningEnabled) {
                    lock (sync) {
                        List<NetConnection> playersWithLoadedLevel = new List<NetConnection>(players.Count);

                        foreach (KeyValuePair<NetConnection, PlayerClient> pair in players) {
                            PlayerClient player = pair.Value;
                            if (player.State != PlayerState.HasLevelLoaded) {
                                continue;
                            }

                            // Initialize player to it can be spawned
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
                            }, 10, pair.Key, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);

                            playersWithLoadedLevel.Clear();
                            foreach (KeyValuePair<NetConnection, PlayerClient> pair2 in players) {
                                if (pair.Key == pair2.Key) {
                                    continue;
                                }

                                if (pair2.Value.State == PlayerState.HasLevelLoaded ||
                                    pair2.Value.State == PlayerState.Spawned ||
                                    pair2.Value.State == PlayerState.Dead) {
                                    playersWithLoadedLevel.Add(pair2.Key);
                                }
                            }

                            string metadataPath;
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
                    }
                }
            }
        }

        private void OnGameLoopThread()
        {
            const int TargetFps = 30;
            const double TargetStep = 1.0 / TargetFps;

            double frequency = (double)Stopwatch.Frequency;
            if (frequency <= 1500) {
                Log.Write(LogType.Warning, "Clock frequency is under 1500 ticks per second. Syncing accuracy issues can be expected.");
            }

            Stopwatch sw = Stopwatch.StartNew();
            double timeAccumulator = 0;
            long prevTicks = sw.ElapsedTicks;

            while (threadGame != null) {
                long currTicks = sw.ElapsedTicks;
                double elapsedTime = Math.Max(currTicks - prevTicks, 0) / frequency;
                timeAccumulator += elapsedTime;
                if (timeAccumulator > 1.0) {
                    timeAccumulator = TargetStep;
                }

                prevTicks = currTicks;

                while (timeAccumulator >= TargetStep) {
                    OnUpdate();

                    timeAccumulator -= TargetStep;
                }

                int frameTime = (int)(((double)(sw.ElapsedTicks - prevTicks) / frequency) * 1000.0);
                frameTime = Math.Max(0, frameTime);

                if (frameTime > 60 && lastGameLoadMs < 60) {
                    Log.Write(LogType.Warning, "Server is overloaded (" + frameTime + " ms per update). For best performance this value should be lower than 30 ms.");
                }

                lastGameLoadMs = frameTime;

                Thread.Sleep(Math.Max(((int)(TargetStep * 1000.0) - frameTime) / 2, 0));
            }
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void OnUpdate()
        {
            Time.FrameTick(false, false);

            AsyncManager.InvokeBeforeUpdate();

            Scene.Current.Update();

            AsyncManager.InvokeAfterUpdate();

            DualityApp.RunCleanup();

            lock (sync) {
                // Respawn dead players immediately
                if (playerSpawningEnabled) {
                    foreach (KeyValuePair<NetConnection, PlayerClient> pair in players) {
                        if (pair.Value.State == PlayerState.Dead) {
                            RespawnPlayer(pair.Value);
                        }
                    }
                }

                // Update all players
                if (playerConnections.Count > 0) {
                    List<ActorBase> spawnedActors = levelHandler.SpawnedActors;

                    int playerCount = players.Count;
                    int spawnedActorCount = spawnedActors.Count;

                    NetOutgoingMessage m = server.CreateMessage(14 + 21 * playerCount + 24 * spawnedActorCount);
                    m.Write(SpecialPacketTypes.UpdateAllActors);
                    m.Write((long)(NetTime.Now * 1000));

                    foreach (KeyValuePair<NetConnection, PlayerClient> pair in players) {
                        PlayerClient player = pair.Value;
                        m.Write((int)player.Index); // Player Index

                        if (player.State != PlayerState.Spawned) {
                            m.Write((byte)0x00); // Flags - None
                            continue;
                        }

                        m.Write((byte)0x01); // Flags - Visible

                        Vector3 pos = player.ProxyActor.Transform.Pos;
                        m.Write((ushort)pos.X);
                        m.Write((ushort)pos.Y);
                        m.Write((ushort)pos.Z);

                        m.Write((bool)player.ProxyActor.IsFacingLeft);
                    }

                    foreach (ActorBase actor in spawnedActors) {
                        if ((actor.CollisionFlags & CollisionFlags.TransformChanged) == 0) {
                            continue;
                        }

                        actor.CollisionFlags &= ~CollisionFlags.TransformChanged;

                        m.Write((int)actor.Index); // Object Index

                        if (actor.Transform.Scale > 0.95f && actor.Transform.Scale < 1.05f &&
                            actor.Transform.Angle > -0.04f && actor.Transform.Angle < 0.04f) {

                            m.Write((byte)0x01); // Flags - Visible

                            Vector3 pos = actor.Transform.Pos;
                            m.Write((ushort)pos.X);
                            m.Write((ushort)pos.Y);
                            m.Write((ushort)pos.Z);

                            m.Write((bool)actor.IsFacingLeft);

                        } else {
                            m.Write((byte)0x03); // Flags - Visible | HasScaleAngle

                            Vector3 pos = actor.Transform.Pos;
                            m.Write((ushort)pos.X);
                            m.Write((ushort)pos.Y);
                            m.Write((ushort)pos.Z);

                            m.Write((float)actor.Transform.Scale);
                            m.WriteRangedSingle((float)actor.Transform.Angle, 0f, MathF.TwoPi, 8);

                            m.Write((bool)actor.IsFacingLeft);
                        }
                    }

                    m.Write((int)-1);

                    // Send update command to all active players
                    server.Send(m, playerConnections, NetDeliveryMethod.Unreliable, PacketChannels.UnorderedUpdates);
                }
            }
        }
        
        public bool ChangeLevel(string levelName, MultiplayerLevelType levelType)
        {
            string path = Path.Combine(DualityApp.DataDirectory, "Episodes", levelName + ".level");
            if (!File.Exists(path)) {
                return false;
            }

            IFileSystem levelPackage = new CompressedContent(path);

            lock (sync) {
                currentLevel = levelName;
                currentLevelType = levelType;

                // ToDo: Better parsing of levelName
                int idx = currentLevel.IndexOf('/');
                levelHandler = new LevelHandler(this, currentLevel.Substring(0, idx), currentLevel.Substring(idx + 1));

                Scene.SwitchTo(levelHandler);

                // Reset active players and send command to change level to all players
                foreach (var player in players) {
                    player.Value.State = PlayerState.NotReady;
                    player.Value.ProxyActor = null;
                    player.Value.CurrentLap = 0;
                    player.Value.CurrentLapTime = 0;
                }

                playerConnections.Clear();

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Level is loaded on server, send request to players to load the level too
                foreach (var player in players) {
                    Send(new LoadLevel {
                        LevelName = currentLevel,
                        LevelType = currentLevelType,
                        AssignedPlayerIndex = player.Value.Index
                    }, 64, player.Key, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
                }

                // ToDo: Do this better
                startTime = NetTime.Now;
            }

            return true;
        }

        public void RespawnPlayer(PlayerClient player)
        {
            lock (sync) {
                if (player.State != PlayerState.HasLevelLoaded && player.State != PlayerState.Dead) {
                    return;
                }

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
                }, 10, player.Connection, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
            }

#if DEBUG
            Log.Write(LogType.Verbose, "[Dev] Respawning player #" + player.Index);
#endif
        }

        public void IncrementPlayerLap(int playerIndex, out int currentLap)
        {
            PlayerClient player = playersByIndex[playerIndex];
            if (player == null) {
                currentLap = -1;
                return;
            }

            double now = NetTime.Now;
            if (player.CurrentLapTime > now - 15) {
                currentLap = -1;
                return;
            }

            player.CurrentLapTime = now;
            player.CurrentLap++;

            currentLap = player.CurrentLap;

#if DEBUG
            Log.Write(LogType.Verbose, "[Dev] Player #" + player.Index + " completed " + currentLap + " laps in " + TimeSpan.FromSeconds(player.CurrentLapTime - startTime));
#endif
        }
    }
}

#endif