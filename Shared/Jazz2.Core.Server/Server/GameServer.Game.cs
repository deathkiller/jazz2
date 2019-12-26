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
using Duality.Resources;
using Jazz2.Actors;
using Jazz2.Game;
using Jazz2.Networking;
using Jazz2.Networking.Packets.Server;
using Lidgren.Network;

namespace Jazz2.Server
{
    partial class GameServer
    {
        public enum ServerState
        {
            Unloaded,
            LevelLoading,
            LevelReady,
            LevelRunning,
            LevelComplete
        }

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
            public PlayerType PlayerType;

            public int StatsDeaths;
            public int StatsKills;
            public int StatsHits;
            public int StatsGems;

            public int CurrentLap;
            public double CurrentLapTime;
            public int RacePosition;

            public Player ProxyActor;
        }

        public struct PlaylistItem
        {
            public string LevelName;
            public MultiplayerLevelType LevelType;
            public int GoalCount;
            public byte PlayerHealth;
        }

        #region JSON
        public class ServerConfigJson
        {
            public string ServerName { get; set; }
            public int Port { get; set; }
            public int MinPlayers { get; set; }
            public int MaxPlayers { get; set; }
            public bool IsPrivate { get; set; }

            public bool PlaylistRandom { get; set; }

            public IList<PlaylistItemJson> Playlist { get; set; }

        }

        public class PlaylistItemJson
        {
            public string LevelName { get; set; }
            public MultiplayerLevelType LevelType { get; set; }
            public int TotalKills { get; set; }
            public int TotalLaps { get; set; }
            public int TotalGems { get; set; }
            public byte PlayerHealth { get; set; }
        }
        #endregion

        private LevelHandler levelHandler;

        private string currentLevel;
        private MultiplayerLevelType currentLevelType;
        private double levelStartTime;
        private ServerState serverState;
        private float countdown;
        private int countdownNotify;

        private List<PlaylistItem> activePlaylist;
        private int activePlaylistIndex;
        private bool activePlaylistRandom;

        private Dictionary<NetConnection, PlayerClient> players;
        public PlayerClient[] playersByIndex;
        private List<NetConnection> playerConnections;
        private byte lastPlayerIndex;
        private bool playerSpawningEnabled = true;
        private byte playerHealth = 5;
        private int raceLastPosition;
        private int raceLastLap;

        private int battleTotalKills = 10;
        private int raceTotalLaps = 3;
        private int treasureHuntTotalGems = 100;

        private int lastFrameTime;

        public ServerState State => serverState;

        public int ActivePlaylistIndex => (activePlaylist == null ? -1 : activePlaylistIndex);

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
                                    Params = new[] { (ushort)player.PlayerType, (ushort)0 }
                                });
                                levelHandler.AddPlayer(player.ProxyActor);
                            } else {
                                if (player.ProxyActor.Health > 0) {
                                    player.ProxyActor.Transform.Pos = pos;
                                } else {
                                    player.ProxyActor.Respawn(pos.Xy);
                                }
                            }

                            player.State = PlayerState.Spawned;

                            Send(new CreateControllablePlayer {
                                Index = player.Index,
                                Type = player.PlayerType,
                                Pos = pos,
                                Health = playerHealth,
                                Controllable = (serverState == ServerState.LevelRunning)
                            }, 11, pair.Key, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);

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

                if (serverState == ServerState.LevelRunning && frameTime > 60 && lastFrameTime < 60) {
                    Log.Write(LogType.Warning, "Server is overloaded (" + frameTime + " ms per update). For best performance this value should be lower than 30 ms.");
                }

                lastFrameTime = frameTime;

                Thread.Sleep(Math.Max(((int)(TargetStep * 1000.0) - frameTime) / 2, 0));
            }
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void OnUpdate()
        {
            if (serverState == ServerState.Unloaded) {
                return;
            }

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

                if (serverState == ServerState.LevelReady) {
                    if (players.Count >= minPlayers) {
                        countdown -= Time.DeltaTime;

                        if (countdown <= 0f) {
                            serverState = ServerState.LevelRunning;
                            countdownNotify = 0;

                            levelStartTime = NetTime.Now;

                            SendToActivePlayers(new PlayerSetControllable {
                                IsControllable = true
                            }, 3, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);

                            SendToActivePlayers(new ShowMessage {
                                Flags = 0x01,
                                Text = "\n\n\n\f[c:1]Go!"
                            }, 24, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);
                        } else if (countdown < countdownNotify) {
                            countdownNotify = (int)Math.Ceiling(countdown);

                            if (countdownNotify == 15) {
                                SendToActivePlayers(new ShowMessage {
                                    Text = "\n\n\n\f[c:1]Game will start in 15 seconds!"
                                }, 48, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);
                            } else if (countdownNotify == 3) {
                                SendToActivePlayers(new ShowMessage {
                                    Flags = 0x01,
                                    Text = "\n\n\n\f[c:4]3"
                                }, 24, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);
                            } else if (countdownNotify == 2) {
                                SendToActivePlayers(new ShowMessage {
                                    Flags = 0x01,
                                    Text = "\n\n\n\f[c:3]2"
                                }, 24, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);
                            } else if (countdownNotify == 1) {
                                SendToActivePlayers(new ShowMessage {
                                    Flags = 0x01,
                                    Text = "\n\n\n\f[c:2]1"
                                }, 24, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);
                            }
                        }
                    }
                } else if (serverState == ServerState.LevelComplete) {
                    countdown -= Time.DeltaTime;

                    if (countdown <= 0f) {
                        if (activePlaylist == null) {
                            ChangeLevel(currentLevel, currentLevelType);
                        } else {
                            ChangeLevelFromPlaylist(activePlaylistIndex + 1);
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

                        if (player.State != PlayerState.Spawned || !player.ProxyActor.IsVisible) {
                            m.Write((byte)0x00); // Flags - None
                            continue;
                        }

                        m.Write((byte)0x01); // Flags - Visible

                        Vector3 pos = player.ProxyActor.Transform.Pos;
                        m.Write((ushort)(pos.X * 2.5f));
                        m.Write((ushort)(pos.Y * 2.5f));
                        m.Write((ushort)(pos.Z * 2.5f));

                        m.Write((bool)player.ProxyActor.IsFacingLeft);
                    }

                    foreach (ActorBase actor in spawnedActors) {
                        if ((actor.CollisionFlags & CollisionFlags.TransformChanged) == 0) {
                            continue;
                        }

                        actor.CollisionFlags &= ~CollisionFlags.TransformChanged;

                        m.Write((int)actor.Index); // Object Index

                        if (!actor.IsVisible) {
                            m.Write((byte)0x00); // Flags - None
                            continue;
                        }

                        if (actor.Transform.Scale > 0.95f && actor.Transform.Scale < 1.05f &&
                            actor.Transform.Angle > -0.04f && actor.Transform.Angle < 0.04f) {

                            m.Write((byte)0x01); // Flags - Visible

                            Vector3 pos = actor.Transform.Pos;
                            m.Write((ushort)(pos.X * 2.5f));
                            m.Write((ushort)(pos.Y * 2.5f));
                            m.Write((ushort)(pos.Z * 2.5f));

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

                    m.Write((int)-1); // Terminator

                    // Send update command to all active players
                    server.Send(m, playerConnections, NetDeliveryMethod.Unreliable, PacketChannels.UnorderedUpdates);
                }
            }
        }
        
        public bool ChangeLevel(string levelName, MultiplayerLevelType levelType, bool fromPlaylist = false)
        {
            if (!fromPlaylist && activePlaylist != null) {
                activePlaylist = null;
                activePlaylistIndex = 0;

                Log.Write(LogType.Info, "Level was changed by administrator. Playlist mode was turned off.");
            }

            string path = Path.Combine(DualityApp.DataDirectory, "Episodes", levelName + ".level");
            if (!File.Exists(path)) {
                return false;
            }

            // This lock will pause main game loop until level is loaded
            lock (sync) {
                currentLevel = levelName;
                currentLevelType = levelType;
                serverState = ServerState.LevelLoading;
                levelStartTime = 0;
                countdown = 600f;
                countdownNotify = int.MaxValue;

                raceLastPosition = 1;

                int idx = currentLevel.IndexOf('/');
                if (idx == -1) {
                    levelHandler = new LevelHandler(this, "unknown", currentLevel);
                } else {
                    levelHandler = new LevelHandler(this, currentLevel.Substring(0, idx), currentLevel.Substring(idx + 1));
                }

                Scene.SwitchTo(levelHandler);

                // Reset active players and send command to change level to all players
                foreach (var player in players) {
                    player.Value.State = PlayerState.NotReady;
                    player.Value.ProxyActor = null;

                    player.Value.CurrentLap = 0;
                    player.Value.CurrentLapTime = 0;
                    player.Value.RacePosition = 0;

                    player.Value.StatsDeaths = 0;
                    player.Value.StatsKills = 0;
                    player.Value.StatsHits = 0;
                }

                playerConnections.Clear();

                // Preload some metadata
                ContentResolver.Current.PreloadAsync("Interactive/PlayerJazz");
                ContentResolver.Current.PreloadAsync("Interactive/PlayerSpaz");
                ContentResolver.Current.PreloadAsync("Interactive/PlayerLori");

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Level is loaded on server, send request to players to load the level too
                foreach (var player in players) {
                    Send(new LoadLevel {
                        ServerName = serverName,
                        LevelName = currentLevel,
                        LevelType = currentLevelType,
                        AssignedPlayerIndex = player.Value.Index
                    }, 64, player.Key, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
                }

                serverState = ServerState.LevelReady;
            }

            return true;
        }

        public void ChangeLevelFromPlaylist(int startIndex)
        {
            if (activePlaylist == null) {
                return;
            }

            if (activePlaylistRandom && activePlaylist.Count > 2) {
                for (int i = 0; i < 3; i++) {
                    int randomIdx = MathF.Rnd.Next(activePlaylist.Count);
                    if (randomIdx == activePlaylistIndex) {
                        continue;
                    }

                    activePlaylistIndex = randomIdx;

                    MultiplayerLevelType levelType = activePlaylist[activePlaylistIndex].LevelType;

                    if (ChangeLevel(activePlaylist[activePlaylistIndex].LevelName, levelType, true)) {
                        int goalCount = activePlaylist[activePlaylistIndex].GoalCount;
                        if (goalCount > 0) {
                            switch (levelType) {
                                case MultiplayerLevelType.Battle:
                                case MultiplayerLevelType.TeamBattle:
                                    battleTotalKills = goalCount; break;
                                case MultiplayerLevelType.Race:
                                    raceTotalLaps = goalCount; break;
                                case MultiplayerLevelType.TreasureHunt:
                                    treasureHuntTotalGems = goalCount; break;
                            }
                        }

                        byte playerHealth = activePlaylist[activePlaylistIndex].PlayerHealth;
                        if (playerHealth > 0) {
                            this.playerHealth = playerHealth;
                        }

                        return;
                    }
                }
            }

            for (int i = 0; i < activePlaylist.Count; i++) {
                activePlaylistIndex = (startIndex + i) % activePlaylist.Count;

                MultiplayerLevelType levelType = activePlaylist[activePlaylistIndex].LevelType;

                if (ChangeLevel(activePlaylist[activePlaylistIndex].LevelName, levelType, true)) {
                    int goalCount = activePlaylist[activePlaylistIndex].GoalCount;
                    if (goalCount > 0) {
                        switch (levelType) {
                            case MultiplayerLevelType.Battle:
                            case MultiplayerLevelType.TeamBattle:
                                battleTotalKills = goalCount; break;
                            case MultiplayerLevelType.Race:
                                raceTotalLaps = goalCount; break;
                            case MultiplayerLevelType.TreasureHunt:
                                treasureHuntTotalGems = goalCount; break;
                        }
                    }

                    byte playerHealth = activePlaylist[activePlaylistIndex].PlayerHealth;
                    if (playerHealth > 0) {
                        this.playerHealth = playerHealth;
                    }
                    break;
                }
            }
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
                        Params = new[] { (ushort)player.PlayerType, (ushort)0 }
                    });
                    levelHandler.AddPlayer(player.ProxyActor);
                } else {
                    if (player.ProxyActor.Health > 0) {
                        player.ProxyActor.Transform.Pos = pos;
                    } else {
                        player.ProxyActor.Respawn(pos.Xy);
                    }
                }

                player.State = PlayerState.Spawned;

                Send(new CreateControllablePlayer {
                    Index = player.Index,
                    Type = player.PlayerType,
                    Pos = pos,
                    Health = playerHealth,
                    Controllable = (serverState == ServerState.LevelRunning)
                }, 11, player.Connection, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
            }

#if DEBUG
            Log.Write(LogType.Verbose, "Respawning player #" + player.Index);
#endif
        }

        public void HandlePlayerDied(int playerIndex)
        {
            if (serverState != ServerState.LevelRunning) {
                return;
            }

            PlayerClient player = playersByIndex[playerIndex];
            if (player == null) {
                return;
            }

            player.State = PlayerState.Dead;
            player.StatsDeaths++;

            Send(new PlayerSetStats {
                Index = (byte)player.Index,
                Kills = player.StatsKills,
                Deaths = player.StatsDeaths
            }, 6, player.Connection, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);

            Send(new ShowMessage {
                Text = "You died " + player.StatsDeaths + " times!"
            }, 24, player.Connection, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);

#if DEBUG
            Log.Write(LogType.Verbose, "Player #" + player.Index + " died");
#endif
        }

        public void IncrementPlayerHits(int victimIndex, int attackerIndex, bool incrementKills)
        {
            if (serverState != ServerState.LevelRunning) {
                return;
            }

            PlayerClient victim = playersByIndex[victimIndex];
            PlayerClient attacker = playersByIndex[attackerIndex];
            if (victim == null || attacker == null) {
                return;
            }

            attacker.StatsHits++;

            if (incrementKills) {
                attacker.StatsKills++;

                Send(new PlayerSetStats {
                    Index = (byte)attacker.Index,
                    Kills = attacker.StatsKills,
                    Deaths = attacker.StatsDeaths
                }, 6, attacker.Connection, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);

                if (currentLevelType == MultiplayerLevelType.Battle && attacker.StatsKills >= battleTotalKills) {
                    // Player won, stop the battle
                    serverState = ServerState.LevelComplete;
                    countdown = 10f;

                    SendToActivePlayers(new PlayerSetControllable {
                        IsControllable = false
                    }, 3, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);

                    Send(new ShowMessage {
                        Flags = 0x01,
                        Text = "\n\n\n\f[c:1]Winner!\n\n\f[s:70]You killed " + attacker.StatsKills + " players."
                    }, 72, attacker.Connection, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);

#if DEBUG
                    Log.Write(LogType.Info, "Player #" + attacker.Index + " won (killed " + attacker.StatsKills + " players in " + TimeSpan.FromSeconds(NetTime.Now - levelStartTime) + ")");
#endif
                } else {
                    Send(new ShowMessage {
                        Text = "You killed " + attacker.StatsKills + " players!"
                    }, 24, attacker.Connection, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);
                }
            }
        }

        public void IncrementPlayerLaps(int playerIndex)
        {
            if (serverState != ServerState.LevelRunning) {
                return;
            }

            PlayerClient player = playersByIndex[playerIndex];
            if (player == null) {
                return;
            }

            double now = NetTime.Now;
            if (player.CurrentLapTime > now - 10) {
                // Lap cannot be shorter than 10 seconds
                return;
            }

            player.CurrentLapTime = now;
            player.CurrentLap++;

            // Number of laps is used only in Race mode
            if (currentLevelType == MultiplayerLevelType.Race) {
                SendToActivePlayers(new PlayerSetLaps {
                    Index = (byte)player.Index,
                    Laps = player.CurrentLap,
                    LapsTotal = raceTotalLaps
                }, 4, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);

                bool isNewLap;
                if (raceLastLap < player.CurrentLap) {
                    raceLastLap = player.CurrentLap;
                    isNewLap = true;
                } else {
                    isNewLap = false;
                }

                if (player.CurrentLap >= raceTotalLaps) {
                    // Player finished all laps
                    lock (sync) {
                        player.RacePosition = raceLastPosition;
                        raceLastPosition++;

                        string placeString;
                        switch (player.RacePosition) {
                            case 1: placeString = "first"; break;
                            case 2: placeString = "second"; break;
                            case 3: placeString = "third"; break;
                            default: placeString = player.RacePosition + "."; break;
                        }

                        Send(new PlayerSetControllable {
                            IsControllable = false
                        }, 3, player.Connection, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);

                        Send(new ShowMessage {
                            Flags = 0x01,
                            Text = "\n\n\n\f[c:1]Finish!\n\n\f[s:70]You won the \f[c:2]" + placeString + "\f[c:1] place."
                        }, 72, player.Connection, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);

#if DEBUG
                        Log.Write(LogType.Verbose, "Player #" + player.Index + " won the " + placeString + " place (completed all laps in " + TimeSpan.FromSeconds(player.CurrentLapTime - levelStartTime) + ")");
#endif

                        bool allFinished = true;
                        foreach (var pair in players) {
                            if (pair.Value.State == PlayerState.Spawned && pair.Value.CurrentLap < raceTotalLaps) {
                                allFinished = false;
                                break;
                            }
                        }

                        if (allFinished) {
                            serverState = ServerState.LevelComplete;
                            countdown = 10f;
#if DEBUG
                            Log.Write(LogType.Info, "All players finished!");
#endif
                        }
                    }
                } else {
                    Send(new ShowMessage {
                        Text = "You completed " + player.CurrentLap + " laps!"
                    }, 24, player.Connection, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);

#if DEBUG
                    Log.Write(LogType.Verbose, "Player #" + player.Index + " completed " + player.CurrentLap + " laps in " + TimeSpan.FromSeconds(player.CurrentLapTime - levelStartTime));
#endif

                    if (isNewLap) {
                        levelHandler.RevertTileMapIfEmpty();
                    }
                }
            }
        }

        public void IncrementPlayerGems(int playerIndex, int count)
        {
            if (serverState != ServerState.LevelRunning) {
                return;
            }

            PlayerClient player = playersByIndex[playerIndex];
            if (player == null) {
                return;
            }

            player.StatsGems += count;

            // Number of laps is used only in Treasure Hunt mode
            if (currentLevelType == MultiplayerLevelType.TreasureHunt) {
                SendToActivePlayers(new PlayerSetLaps {
                    Index = (byte)player.Index,
                    Laps = player.StatsGems,
                    LapsTotal = treasureHuntTotalGems
                }, 4, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);

                if (player.StatsGems >= treasureHuntTotalGems) {
                    // Player collected all gems
                    serverState = ServerState.LevelComplete;
                    countdown = 10f;

                    SendToActivePlayers(new PlayerSetControllable {
                        IsControllable = false
                    }, 3, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);

                    Send(new ShowMessage {
                        Flags = 0x01,
                        Text = "\n\n\n\f[c:1]Winner!\n\n\f[s:70]You collected " + player.StatsGems + " gems."
                    }, 72, player.Connection, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);

#if DEBUG
                    Log.Write(LogType.Verbose, "Player #" + player.Index + " collected all gems in " + TimeSpan.FromSeconds(NetTime.Now - levelStartTime));
#endif
                }
            }
        }
    }
}

#endif