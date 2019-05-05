#if MULTIPLAYER

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Duality;
using Jazz2.Actors;
using Jazz2.Game.Structs;
using Jazz2.Networking.Packets;
using Jazz2.Networking.Packets.Server;
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

        public class Player
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

            public bool IsFirePressed;
            public float WeaponCooldown;
        }

        public class Object
        {
            public int Index;

            public string Metadata;

            public Vector3 Pos;
            public Vector2 Speed;

            public AnimState AnimState;
            public float AnimTime;
            public bool IsFacingLeft;

            public float TimeLeft;
        }

        private string currentLevel = "unknown/battle2";

        private Dictionary<NetConnection, Player> players;
        private List<NetConnection> playerConnections;
        private byte lastPlayerIndex;

        private Dictionary<int, Object> objects;
        private int lastObjectIndex;

        private int lastGameLoadMs;


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
                                pair.Value.WeaponCooldown = 35f;

                                SpawnObject("Weapon/Blaster", pair.Value.Pos, pair.Value.Speed + new Vector2(pair.Value.IsFacingLeft ? -10f : 10f, 0f));
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
                        }

                        server.Send(m, playerConnections, NetDeliveryMethod.Unreliable, PacketChannels.Main);
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
                success = objects.Remove(index);
            }

            if (success) {
                Send(new DestroyRemoteObject {
                    Index = index,
                }, 5, playerConnections, NetDeliveryMethod.ReliableUnordered, PacketChannels.Main);
            }
        }
    }
}

#endif