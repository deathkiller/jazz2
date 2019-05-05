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
            Spawned
        }

        public class Player : ICollisionable
        {
            public byte Index;
            public PlayerState State;
            public long LastUpdateTime;

            public Vector3 Pos;
            public Vector2 Speed;

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

            private int proxyId = -1;
            private AABB aabb;

            public ref int ProxyId => ref proxyId;
            public ref AABB AABB => ref aabb;
        }

        private string currentLevel;

        private Dictionary<NetConnection, Player> players;
        private List<NetConnection> playerConnections;
        private byte lastPlayerIndex;
        private bool enablePlayerSpawning = true;

        private Dictionary<int, Object> objects;
        private int lastObjectIndex;

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
                float timeMult = Time.TimeMult;

                lock (sync) {
                    // Update objects
                    foreach (KeyValuePair<NetConnection, Player> pair in players) {
                        if (pair.Value.IsFirePressed) {
                            if (pair.Value.WeaponCooldown <= 0f) {
                                if (pair.Value.Controllable) {
                                    pair.Value.WeaponCooldown = 35f;

                                    SpawnObject("Weapon/Blaster", pair.Value.Pos, pair.Value.Speed + new Vector2(pair.Value.IsFacingLeft ? -10f : 10f, 0f));
                                }
                            } else {
                                pair.Value.WeaponCooldown -= 1f * timeMult;
                            }
                        } else {
                            pair.Value.WeaponCooldown = 0f;
                        }
                    }

                    foreach (KeyValuePair<int, Object> pair in objects) {
                        if (pair.Value.TimeLeft <= 0f) {
                            DestroyObject(pair.Key);
                            break;
                        }

                        pair.Value.Pos.X += pair.Value.Speed.X * timeMult;
                        pair.Value.Pos.Y += pair.Value.Speed.Y * timeMult;

                        pair.Value.TimeLeft -= 1f * timeMult;
                    }

                    // Update all players
                    if (playerConnections.Count > 0) {
                        int playerCount = players.Count;
                        int objectCount = objects.Count;

                        NetOutgoingMessage m = server.CreateMessage(14 + 21 * playerCount + 24 * objectCount);
                        m.Write(PacketTypes.UpdateAll);
                        m.Write((long)(NetTime.Now * 1000));

                        m.Write((byte)playerCount);
                        foreach (KeyValuePair<NetConnection, Player> pair in players) {
                            Player p = pair.Value;
                            m.Write((byte)p.Index); // Player Index

                            if (p.State != PlayerState.Spawned) {
                                m.Write((byte)0); // Flags - None
                                continue;
                            }

                            m.Write((byte)1); // Flags - Spawned

                            m.Write((ushort)p.Pos.X);
                            m.Write((ushort)p.Pos.Y);
                            m.Write((ushort)p.Pos.Z);

                            m.Write((short)(p.Speed.X * 500f));
                            m.Write((short)(p.Speed.Y * 500f));

                            m.Write((uint)p.AnimState);
                            m.Write((float)p.AnimTime);
                            m.Write((bool)p.IsFacingLeft);

                            AABB aabb = new AABB(p.Pos.Xy, 20, 30);
                            collisions.MoveProxy(p, ref aabb, p.Speed);
                            p.AABB = aabb;
                        }

                        m.Write((int)objectCount);
                        foreach (KeyValuePair<int, Object> pair in objects) {
                            Object o = pair.Value;
                            m.Write((int)o.Index); // Player Index

                            m.Write((byte)0); // Flags - None

                            m.Write((ushort)o.Pos.X);
                            m.Write((ushort)o.Pos.Y);
                            m.Write((ushort)o.Pos.Z);

                            m.Write((short)(o.Speed.X * 500f));
                            m.Write((short)(o.Speed.Y * 500f));

                            m.Write((uint)o.AnimState);
                            m.Write((float)o.AnimTime);
                            m.Write((bool)o.IsFacingLeft);

                            AABB aabb = new AABB(o.Pos.Xy, 8, 8);
                            collisions.MoveProxy(o, ref aabb, o.Speed);
                            o.AABB = aabb;
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
        
        public bool ChangeLevel(string levelName)
        {
            string path = Path.Combine(DualityApp.DataDirectory, "Episodes", levelName + ".level");
            if (!File.Exists(path)) {
                return false;
            }

            IFileSystem levelPackage = new CompressedContent(path);

            lock (sync) {
                currentLevel = levelName;

                // Load new level
                using (Stream s = levelPackage.OpenFile(".res", FileAccessMode.Read)) {
                    // ToDo: Cache parser, move JSON parsing to ContentResolver
                    JsonParser json = new JsonParser();
                    LevelHandler.LevelConfigJson config = json.Parse<LevelHandler.LevelConfigJson>(s);

                    if (config.Version.LayerFormat > LevelHandler.LayerFormatVersion || config.Version.EventSet > LevelHandler.EventSetVersion) {
                        throw new NotSupportedException("Version not supported");
                    }

                    //App.Log("Loading level \"" + config.Description.Name + "\"...");

                    //root.Title = BitmapFont.StripFormatting(config.Description.Name);
                    //root.Immersive = false;
                    Log.Write(LogType.Info, "Loading level \"" + config.Description.Name + "\"...");

                    //defaultNextLevel = config.Description.NextLevel;
                    //defaultSecretLevel = config.Description.SecretLevel;
                    //ambientLightDefault = config.Description.DefaultLight;
                    //ambientLightCurrent = ambientLightTarget = ambientLightDefault * 0.01f;

                    //if (config.Description.DefaultDarkness != null && config.Description.DefaultDarkness.Count >= 4) {
                    //    darknessColor = new Vector4(config.Description.DefaultDarkness[0] / 255f, config.Description.DefaultDarkness[1] / 255f, config.Description.DefaultDarkness[2] / 255f, config.Description.DefaultDarkness[3] / 255f);
                    //} else {
                    //    darknessColor = new Vector4(0, 0, 0, 1);
                    //}

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

                    //levelTexts = config.TextEvents ?? new Dictionary<int, string>();
                }

                // Send request to change level to all players
                foreach (KeyValuePair<NetConnection, Player> pair in players) {
                    pair.Value.State = PlayerState.NotReady;
                }

                playerConnections.Clear();

                foreach (KeyValuePair<NetConnection, Player> pair in players) {
                    Send(new LoadLevel {
                        LevelName = currentLevel,
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
                    player.PlayerType = MathF.Rnd.OneOf(new[] { PlayerType.Jazz, PlayerType.Spaz, PlayerType.Lori, PlayerType.Frog });

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

                        if (pair2.Value.State == PlayerState.HasLevelLoaded || pair2.Value.State == PlayerState.Spawned) {
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

        public void SpawnObject(string metadata, Vector3 pos, Vector2 speed)
        {
            Object newObject = new Object {
                Metadata = metadata,
                Pos = pos,
                Speed = speed,

                TimeLeft = 100f
            };

            lock (sync) {
                newObject.Index = lastObjectIndex;
                lastObjectIndex++;

                objects[newObject.Index] = newObject;

                newObject.AABB = new AABB(newObject.Pos.Xy, 20, 30);
                collisions.AddProxy(newObject);
            }

            Send(new CreateRemoteObject {
                Index = newObject.Index,
                Metadata = newObject.Metadata,
                Pos = newObject.Pos,
            }, 10 + metadata.Length * 2, playerConnections, NetDeliveryMethod.ReliableUnordered, PacketChannels.Main);
        }

        public void DestroyObject(int index)
        {
            bool success;

            lock (sync) {
                Object oldObject;
                if (objects.TryGetValue(index, out oldObject)) {
                    collisions.RemoveProxy(oldObject);
                }

                success = objects.Remove(index);
            }

            if (success) {
                Send(new DestroyRemoteObject {
                    Index = index,
                }, 5, playerConnections, NetDeliveryMethod.ReliableUnordered, PacketChannels.Main);
            }
        }

        private void ResolveCollisions()
        {
            collisions.UpdatePairs((proxyA, proxyB) => {

                /*if (proxyA.Health <= 0 || proxyB.Health <= 0) {
                    return;
                }

                if (proxyA.IsCollidingWith(proxyB)) {
                    proxyA.OnHandleCollision(proxyB);
                    proxyB.OnHandleCollision(proxyA);

                    collisionsCountC++;
                }

                collisionsCountB++;*/

                CheckCollisions(proxyA, proxyB);
                CheckCollisions(proxyB, proxyA);

                //Log.Write(LogType.Verbose, "Collision: " + proxyA.GetType() + " | " + proxyB.GetType());
            });
        }

        private void CheckCollisions(ICollisionable proxyA, ICollisionable proxyB)
        {
            Player p = proxyA as Player;
            if (p != null) {
                Object o = proxyB as Object;
                if (o != null) {
                    DestroyObject(o.Index);

                    // ToDo: Send it only to target player
                    Send(new DecreasePlayerHealth {
                        Index = p.Index,
                        Amount = 1
                    }, 3, playerConnections, NetDeliveryMethod.ReliableUnordered, PacketChannels.Main);
                }
            }
        }
    }
}

#endif