#if MULTIPLAYER

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Duality;
using Jazz2.Actors;
using Jazz2.Game.Structs;
using Jazz2.Networking.Packets;
using Lidgren.Network;

namespace Jazz2.Server
{
    partial class App
    {
        private class Player
        {
            public byte Index;
            public long LastUpdateTime;

            public Vector3 Pos;
            public Vector2 Speed;

            public AnimState AnimState;
            public float AnimTime;
            public bool IsFacingLeft;

            public bool HasLevelLoaded;

            public PlayerType PlayerType;
            public int Lives, Coins, Gems, FoodEaten;
            public float InvulnerableTime;
        }


        private static void OnGameLoop()
        {
            const int TargetFps = 30;

            Stopwatch sw = new Stopwatch();

            while (threadGame != null) {
                sw.Restart();

                // ToDo: Update components

                // Update all players if there is more than one player
                if (playerConnections.Count > 1) {
                    int playerCount = players.Count;
                    NetOutgoingMessage m = server.CreateMessage(10 + 19 * playerCount);
                    m.Write(PacketTypes.UpdateAllPlayers);
                    m.Write((long)(NetTime.Now * 1000));
                    m.Write((byte)playerCount);

                    foreach (KeyValuePair<NetConnection, Player> pair in players) {
                        Player player = pair.Value;
                        m.Write((byte)player.Index); // Player Index

                        if (!player.HasLevelLoaded) {
                            m.Write((byte)0); // Flags - None
                            continue;
                        }

                        m.Write((byte)1); // Flags - Spawned

                        m.Write((ushort)player.Pos.X);
                        m.Write((ushort)player.Pos.Y);
                        m.Write((ushort)player.Pos.Z);

                        m.Write((short)(player.Speed.X * 500f));
                        m.Write((short)(player.Speed.Y * 500f));

                        m.Write((uint)player.AnimState);
                        m.Write((float)player.AnimTime);
                        m.Write((bool)player.IsFacingLeft);
                    }

                    server.Send(m, playerConnections, NetDeliveryMethod.Unreliable, PacketChannels.Main);
                }

                sw.Stop();

                lastGameLoadMs = (int)sw.ElapsedMilliseconds;
                int sleepMs = 1000 / TargetFps - lastGameLoadMs;
                if (sleepMs > 0) {
                    Thread.Sleep(sleepMs);
                }
            }
        }
    }
}

#endif