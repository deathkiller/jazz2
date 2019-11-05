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

            public Player ProxyActor;
        }

        private LevelHandler levelHandler;

        private string currentLevel;
        private string currentLevelFriendlyName;
        private MultiplayerLevelType currentLevelType;

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
                                player.ProxyActor.OnActivated(new ActorActivationDetails {
                                    LevelHandler = levelHandler,
                                    Pos = pos,
                                    Params = new[] { (ushort)MathF.Rnd.OneOf(new[] { PlayerType.Jazz, PlayerType.Spaz, PlayerType.Lori }), (ushort)/*player.Index*/0 }
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
                            }, 9, pair.Key, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);

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

                            // Send command to all active players to create this new player
                            Send(new CreateRemotePlayer {
                                Index = player.Index,
                                Type = player.ProxyActor.PlayerType,
                                Pos = pos
                            }, 9, playersWithLoadedLevel, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
                        }
                    }
                }
            }
        }

        private void OnGameLoop()
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

            //Scene.Current.Update();

            /*lock (sync) {
                for (int i = 0; i < spawnedActors.Count; i++) {
                    ActorBase actor = spawnedActors[i];
                    actor.OnUpdate();
                    actor.OnFixedUpdate(1f);
                }
            }*/

            if (levelHandler != null) {
                levelHandler.Update();
            }

            AsyncManager.InvokeAfterUpdate();

            DualityApp.RunCleanup();

            lock (sync) {
                // Respawn dead players immediately
                foreach (KeyValuePair<NetConnection, PlayerClient> pair in players) {
                    if (pair.Value.State == PlayerState.Dead) {
                        if (playerSpawningEnabled) {
                            RespawnPlayer(pair.Value);
                        }
                        //continue;
                    }
                    //else if (pair.Value.State != PlayerState.Spawned) {
                    //    continue;
                    //}
                }

                // Update all players
                if (playerConnections.Count > 0) {
                    List<ActorBase> spawnedActors = levelHandler.SpawnedActors;

                    int playerCount = players.Count;
                    //int remotableActorCount = remotableActors.Count;
                    int spawnedActorCount = spawnedActors.Count;

                    NetOutgoingMessage m = server.CreateMessage(14 + 21 * playerCount + 24 * spawnedActorCount);
                    m.Write(SpecialPacketTypes.UpdateAll);
                    m.Write((long)(NetTime.Now * 1000));

                    m.Write((byte)playerCount);
                    foreach (KeyValuePair<NetConnection, PlayerClient> pair in players) {
                        PlayerClient player = pair.Value;
                        m.Write((byte)player.Index); // Player Index

                        if (player.State != PlayerState.Spawned) {
                            m.Write((byte)0); // Flags - None
                            continue;
                        }

                        m.Write((byte)1); // Flags - Spawned

                        Vector3 pos = player.ProxyActor.Transform.Pos;
                        m.Write((ushort)pos.X);
                        m.Write((ushort)pos.Y);
                        m.Write((ushort)pos.Z);

                        m.Write((uint)player.ProxyActor.AnimState);
                        m.Write((float)player.ProxyActor.AnimTime);
                        m.Write((bool)player.ProxyActor.IsFacingLeft);

                        //AABB aabb = new AABB(player.Pos.Xy, 20, 30);
                        //collisions.MoveProxy(player, ref aabb, player.Speed);
                        //player.AABB = aabb;
                    }

                    //m.Write((int)remotableActorCount);
                    //foreach (KeyValuePair<int, RemotableActor> pair in remotableActors) {
                    m.Write((int)spawnedActorCount);
                    foreach (ActorBase actor in spawnedActors) {
                        m.Write((int)actor.Index); // Object Index

                        m.Write((byte)0); // Flags - None

                        m.Write((ushort)actor.Transform.Pos.X);
                        m.Write((ushort)actor.Transform.Pos.Y);
                        m.Write((ushort)actor.Transform.Pos.Z);

                        m.Write((short)(actor.Speed.X * 500f));
                        m.Write((short)(actor.Speed.Y * 500f));

                        //m.Write((uint)actor.AnimState);
                        //m.Write((float)actor.AnimTime);
                        m.Write((bool)true); // ToDo
                        m.Write((bool)actor.IsFacingLeft);

                        //AABB aabb = new AABB(actor.Pos.Xy, 8, 8);
                        //collisions.MoveProxy(actor, ref aabb, actor.Speed);
                        //actor.AABB = aabb;
                    }

                    // Send update command to all active players
                    server.Send(m, playerConnections, NetDeliveryMethod.Unreliable, PacketChannels.UnreliableUpdates);
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

                // Load new level
                /*using (Stream s = levelPackage.OpenFile(".res", FileAccessMode.Read)) {
                    // ToDo: Cache parser
                    JsonParser json = new JsonParser();
                    LevelHandler.LevelConfigJson config = json.Parse<LevelHandler.LevelConfigJson>(s);

                    if (config.Version.LayerFormat > LevelHandler.LayerFormatVersion || config.Version.EventSet > LevelHandler.EventSetVersion) {
                        throw new NotSupportedException("Version not supported");
                    }

                    // TODO
                    //currentLevelFriendlyName = BitmapFont.StripFormatting(config.Description.Name);
                    currentLevelFriendlyName = config.Description.Name;

                    Log.Write(LogType.Info, "Loading level \"" + currentLevelFriendlyName + "\" (" + currentLevelType + ")...");

                    // Palette
                    ColorRgba[] tileMapPalette;
                    if (levelPackage.FileExists("Main.palette")) {
                        using (Stream s2 = levelPackage.OpenFile("Main.palette", FileAccessMode.Read)) {
                            tileMapPalette = TileSet.LoadPalette(s2);
                        }
                    } else {
                        tileMapPalette = null;
                    }

                    // Tileset
                    tileMap = new TileMap(levelHandler, config.Description.DefaultTileset, tileMapPalette, (config.Description.Flags & LevelHandler.LevelFlags.HasPit) != 0);

                    // Additional tilesets
                    if (config.Tilesets != null) {
                        for (int i = 0; i < config.Tilesets.Count; i++) {
                            LevelHandler.LevelConfigJson.TilesetSection part = config.Tilesets[i];
                            tileMap.ReadTilesetPart(part.Name, part.Offset, part.Count);
                        }
                    }

                    Point2 tileMapSize;
                    using (Stream s2 = levelPackage.OpenFile("Sprite.layer", FileAccessMode.Read))
                    using (BinaryReader r = new BinaryReader(s2)) {
                        tileMapSize.X = r.ReadInt32();
                        tileMapSize.Y = r.ReadInt32();
                    }

                    levelBounds = new Rect(tileMapSize * 32);

                    collisions = new DynamicTreeBroadPhase<ICollisionable>();

                    // Events
                    eventMap = new ServerEventMap(this, tileMapSize);

                    if (levelPackage.FileExists("Events.layer")) {
                        using (Stream s2 = levelPackage.OpenFile("Events.layer", FileAccessMode.Read)) {
                            eventMap.ReadEvents(s2, config.Version.LayerFormat);
                        }
                    }
                }

                spawnedActors.Clear();
                spawnedActorsAnimation.Clear();
                lastSpawnedActorId = 0;

                eventMap.ActivateEvents();*/

                // ToDo
                int idx = currentLevel.IndexOf('/');
                levelHandler = new LevelHandler(this, currentLevel.Substring(0, idx), currentLevel.Substring(idx + 1));

                Scene.SwitchTo(levelHandler);


                // Reset active players and send command to change level to all players
                foreach (KeyValuePair<NetConnection, PlayerClient> pair in players) {
                    pair.Value.State = PlayerState.NotReady;
                    pair.Value.ProxyActor = null;
                }

                playerConnections.Clear();

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();
                GC.WaitForPendingFinalizers();

                foreach (KeyValuePair<NetConnection, PlayerClient> pair in players) {
                    Send(new LoadLevel {
                        LevelName = currentLevel,
                        LevelType = currentLevelType,
                        AssignedPlayerIndex = pair.Value.Index
                    }, 64, pair.Key, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
                }
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
                    player.ProxyActor.OnActivated(new ActorActivationDetails {
                        LevelHandler = levelHandler,
                        Pos = pos,
                        Params = new[] { (ushort)MathF.Rnd.OneOf(new[] { PlayerType.Jazz, PlayerType.Spaz, PlayerType.Lori }), (ushort)/*player.Index*/0 }
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
                }, 9, player.Connection, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
            }
        }
    }
}

#endif