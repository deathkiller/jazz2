#if MULTIPLAYER

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using Duality;
using Duality.IO;
using Jazz2.Actors;
using Jazz2.Game;
using Jazz2.Game.Collisions;
using Jazz2.Game.Structs;
using Jazz2.Game.UI;
using Jazz2.Networking;
using Jazz2.Networking.Packets;
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

        public class Player : ICollisionable
        {
            public NetConnection Connection;
            public byte[] ClientIdentifier;

            public byte Index;
            public PlayerState State;
            public long LastUpdateTime;

            public Vector3 Pos;

            public AnimState AnimState;
            public float AnimTime;
            public bool IsFacingLeft;

            public PlayerType PlayerType;
            public int Lives, Coins, Gems, FoodEaten;
            public float InvulnerableTime;

            public bool Controllable;
            public bool IsFirePressed;
            public float WeaponCooldown;

            private int proxyId = -1;
            private AABB aabb;

            public ref int ProxyId => ref proxyId;
            public ref AABB AABB => ref aabb;
        }

        public class Object : ICollisionable
        {
            public int Index;

            public string Metadata;

            public Vector3 Pos;
            public Vector2 Speed;

            public AnimState AnimState;
            public float AnimTime;
            public bool IsFacingLeft;

            public float TimeLeft;

            public Player Owner;

            private int proxyId = -1;
            private AABB aabb;

            public ref int ProxyId => ref proxyId;
            public ref AABB AABB => ref aabb;
        }

        public class RemotableActor : ICollisionable
        {
            public int Index;
            public EventType EventType;
            public ushort[] EventParams;
            public long LastUpdateTime;

            public Vector3 Pos;
            public Vector2 Speed;

            public AnimState AnimState;
            public float AnimTime;
            public bool IsFacingLeft;

            public Player Owner;

            private int proxyId = -1;
            private AABB aabb;

            public ref int ProxyId => ref proxyId;
            public ref AABB AABB => ref aabb;
        }

        private string currentLevel;
        private MultiplayerLevelType currentLevelType;

        private Dictionary<NetConnection, Player> players;
        private List<NetConnection> playerConnections;
        private byte lastPlayerIndex;
        private bool enablePlayerSpawning = true;

        private Dictionary<int, RemotableActor> remotableActors;

        private int lastGameLoadMs;

        private Rect levelBounds;
        private ServerEventMap eventMap;
        private DynamicTreeBroadPhase<ICollisionable> collisions = new DynamicTreeBroadPhase<ICollisionable>();


        private void OnGameLoop()
        {
            const int TargetFps = 30;

            Stopwatch sw = new Stopwatch();

            while (threadGame != null) {
                sw.Restart();

                // Update time
                Time.FrameTick(false, false);

                lock (sync) {
                    // Update objects
                    foreach (KeyValuePair<NetConnection, Player> pair in players) {
                        if (pair.Value.State == PlayerState.Dead) {
                            if (enablePlayerSpawning) {
                                RespawnPlayer(pair.Value);
                            }
                            continue;
                        } else if (pair.Value.State != PlayerState.Spawned) {
                            continue;
                        }
                    }

                    // Update all players
                    if (playerConnections.Count > 0) {
                        int playerCount = players.Count;
                        int actorCount = remotableActors.Count;

                        NetOutgoingMessage m = server.CreateMessage(14 + 21 * playerCount + 24 * actorCount);
                        m.Write(PacketTypes.UpdateAll);
                        m.Write((long)(NetTime.Now * 1000));

                        m.Write((byte)playerCount);
                        foreach (KeyValuePair<NetConnection, Player> pair in players) {
                            Player player = pair.Value;
                            m.Write((byte)player.Index); // Player Index

                            if (player.State != PlayerState.Spawned) {
                                m.Write((byte)0); // Flags - None
                                continue;
                            }

                            m.Write((byte)1); // Flags - Spawned

                            m.Write((ushort)player.Pos.X);
                            m.Write((ushort)player.Pos.Y);
                            m.Write((ushort)player.Pos.Z);

                            m.Write((uint)player.AnimState);
                            m.Write((float)player.AnimTime);
                            m.Write((bool)player.IsFacingLeft);

                            //AABB aabb = new AABB(player.Pos.Xy, 20, 30);
                            //collisions.MoveProxy(player, ref aabb, player.Speed);
                            //player.AABB = aabb;
                        }

                        m.Write((int)actorCount);
                        foreach (KeyValuePair<int, RemotableActor> pair in remotableActors) {
                            RemotableActor actor = pair.Value;
                            m.Write((int)actor.Index); // Object Index

                            m.Write((byte)0); // Flags - None

                            m.Write((ushort)actor.Pos.X);
                            m.Write((ushort)actor.Pos.Y);
                            m.Write((ushort)actor.Pos.Z);

                            m.Write((short)(actor.Speed.X * 500f));
                            m.Write((short)(actor.Speed.Y * 500f));

                            m.Write((uint)actor.AnimState);
                            m.Write((float)actor.AnimTime);
                            m.Write((bool)actor.IsFacingLeft);

                            //AABB aabb = new AABB(actor.Pos.Xy, 8, 8);
                            //collisions.MoveProxy(actor, ref aabb, actor.Speed);
                            //actor.AABB = aabb;
                        }

                        server.Send(m, playerConnections, NetDeliveryMethod.Unreliable, PacketChannels.Main);

                        ResolveCollisions();
                    }
                }

                sw.Stop();

                lastGameLoadMs = (int)sw.ElapsedMilliseconds;
                int sleepMs = (1000 / TargetFps) - lastGameLoadMs;
                if (sleepMs > 0) {
                    Thread.Sleep(sleepMs);
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
                using (Stream s = levelPackage.OpenFile(".res", FileAccessMode.Read)) {
                    // ToDo: Cache parser
                    JsonParser json = new JsonParser();
                    LevelHandler.LevelConfigJson config = json.Parse<LevelHandler.LevelConfigJson>(s);

                    if (config.Version.LayerFormat > LevelHandler.LayerFormatVersion || config.Version.EventSet > LevelHandler.EventSetVersion) {
                        throw new NotSupportedException("Version not supported");
                    }

                    Log.Write(LogType.Info, "Loading level \"" + BitmapFont.StripFormatting(config.Description.Name) + "\" (" + currentLevelType + ")...");

                    Point2 tileMapSize;
                    using (Stream s2 = levelPackage.OpenFile("Sprite.layer", FileAccessMode.Read))
                    using (BinaryReader r = new BinaryReader(s2)) {
                        tileMapSize.X = r.ReadInt32();
                        tileMapSize.Y = r.ReadInt32();
                    }

                    levelBounds = new Rect(tileMapSize * /*tileMap.Tileset.TileSize*/32);

                    collisions = new DynamicTreeBroadPhase<ICollisionable>();

                    // Read events
                    eventMap = new ServerEventMap(tileMapSize);

                    if (levelPackage.FileExists("Events.layer")) {
                        using (Stream s2 = levelPackage.OpenFile("Events.layer", FileAccessMode.Read)) {
                            eventMap.ReadEvents(s2, config.Version.LayerFormat);
                        }
                    }
                }

                // Send request to change level to all players
                foreach (KeyValuePair<NetConnection, Player> pair in players) {
                    pair.Value.State = PlayerState.NotReady;
                }

                playerConnections.Clear();

                foreach (KeyValuePair<NetConnection, Player> pair in players) {
                    Send(new LoadLevel {
                        LevelName = currentLevel,
                        LevelType = currentLevelType,
                        AssignedPlayerIndex = pair.Value.Index
                    }, 64, pair.Key, NetDeliveryMethod.ReliableUnordered, PacketChannels.Main);
                }
            }

            return true;
        }

        public void EnablePlayerSpawning(bool enable)
        {
            if (enablePlayerSpawning == enable) {
                return;
            }

            enablePlayerSpawning = enable;

            if (enablePlayerSpawning) {
                List<NetConnection> playersWithLoadedLevel = new List<NetConnection>();
                
                foreach (KeyValuePair<NetConnection, Player> pair in players) {
                    Player player = pair.Value;
                    if (player.State != PlayerState.HasLevelLoaded) {
                        continue;
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
                    }, 9, pair.Key, NetDeliveryMethod.ReliableUnordered, PacketChannels.Main);

                    playersWithLoadedLevel.Clear();
                    foreach (KeyValuePair<NetConnection, Player> pair2 in players) {
                        if (pair.Key == pair2.Key) {
                            continue;
                        }

                        if (pair2.Value.State == PlayerState.HasLevelLoaded || pair2.Value.State == PlayerState.Spawned || pair2.Value.State == PlayerState.Dead) {
                            playersWithLoadedLevel.Add(pair2.Key);
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

        public void RespawnPlayer(Player player)
        {
            lock (sync) {
                if (player.State != PlayerState.HasLevelLoaded && player.State != PlayerState.Dead) {
                    return;
                }

                player.State = PlayerState.Spawned;
                player.Pos.Xy = eventMap.GetRandomSpawnPosition();

                Send(new CreateControllablePlayer {
                    Index = player.Index,
                    Type = player.PlayerType,
                    Pos = player.Pos
                }, 9, player.Connection, NetDeliveryMethod.ReliableUnordered, PacketChannels.Main);
            }
        }

        private void ResolveCollisions()
        {
            collisions.UpdatePairs((proxyA, proxyB) => {
                CheckCollisions(proxyA, proxyB);
                CheckCollisions(proxyB, proxyA);
            });
        }

        private void CheckCollisions(ICollisionable proxyA, ICollisionable proxyB)
        {
            // ToDo
        }
    }
}

#endif