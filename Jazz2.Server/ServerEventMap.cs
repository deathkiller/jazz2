#if MULTIPLAYER

using System.Collections.Generic;
using System.IO;
using Duality;
using Jazz2.Actors;
using Jazz2.Game;
using Jazz2.Game.Structs;

namespace Jazz2.Server
{
    public class ServerEventMap
    {
        private struct EventTile
        {
            public EventType EventType;
            public ActorInstantiationFlags EventFlags;
            public ushort[] EventParams;
            public bool IsEventActive;

            public float Delay;
            public float TimeLeft;

            public ActorBase SpawnedActor;
        }

        private GameServer server;
        private EventTile[] eventLayout;
        private int layoutWidth, layoutHeight;

        private List<Vector2> spawnPositions = new List<Vector2>();

        public ServerEventMap(GameServer server, Point2 size)
        {
            this.server = server;

            layoutWidth = size.X;
            layoutHeight = size.Y;

            eventLayout = new EventTile[size.X * size.Y];
        }

        public void ReadEvents(Stream s, uint layoutVersion)
        {
            using (BinaryReader r = new BinaryReader(s)) {
                int width = r.ReadInt32();
                int height = r.ReadInt32();

                byte difficultyBit = 5; // Normal difficulty

                for (int y = 0; y < height; y++) {
                    for (int x = 0; x < width; x++) {
                        ushort eventID = r.ReadUInt16();
                        byte flags = r.ReadByte();
                        ushort[] eventParams = new ushort[8];

                        // ToDo: Remove inlined constants

                        // Flag 0x02: Generator
                        byte generatorFlags, generatorDelay;
                        if ((flags & 0x02) != 0) {
                            //eventFlags ^= 0x02;
                            generatorFlags = r.ReadByte();
                            generatorDelay = r.ReadByte();
                        } else {
                            generatorFlags = 0;
                            generatorDelay = 0;
                        }

                        // Flag 0x01: No params provided
                        if ((flags & 0x01) == 0) {
                            flags ^= 0x01;
                            for (int i = 0; i < 8; ++i) {
                                eventParams[i] = r.ReadUInt16();
                            }
                        }

                        ActorInstantiationFlags eventFlags = (ActorInstantiationFlags)(flags & 0x04);

                        // Flag 0x02: Generator
                        if ((flags & 0x02) != 0) {
                            if ((flags & (0x01 << difficultyBit)) != 0 || (flags & 0x80) != 0) {
                                // ToDo
                                float timeLeft = ((generatorFlags & 0x01) != 0 ? generatorDelay : 0f);

                                StoreTileEvent(x, y, (EventType)eventID, eventFlags, eventParams, generatorDelay, timeLeft);
                            }
                            continue;
                        }

                        // If the difficulty bytes for the event don't match the selected difficulty, don't add anything to the event map.
                        // Additionally, never show events that are multiplayer-only for now.
                        if (flags == 0 || (flags & (0x01 << difficultyBit)) != 0 || (flags & 0x80) != 0) {
                            switch ((EventType)eventID) {
                                case EventType.Empty:
                                    break;

                                case EventType.LevelStart: {
                                    //for (int i = 0; i < /*16*/4; i++) {
                                    //    if ((eventParams[0] & (1 << i)) != 0) { // Bitmask - 1: Jazz, 2: Spaz, 4: Lori
                                    //        AddSpawnPosition((PlayerType)i, x, y);
                                    //    }
                                    //}
                                    AddSpawnPosition(x, y);
                                    break;
                                }

                                case EventType.ModifierOneWay:
                                case EventType.ModifierVine:
                                case EventType.ModifierHook:
                                case EventType.ModifierHurt:
                                case EventType.SceneryDestruct:
                                case EventType.SceneryDestructButtstomp:
                                case EventType.TriggerArea:
                                case EventType.SceneryDestructSpeed:
                                case EventType.SceneryCollapse:
                                case EventType.ModifierHPole:
                                case EventType.ModifierVPole: {
                                    //StoreTileEvent(x, y, (EventType)eventID, eventFlags, eventParams);
                                    //TileMap tiles = levelHandler.TileMap;
                                    //if (tiles != null) {
                                    //    tiles.SetTileEventFlags(x, y, (EventType)eventID, eventParams);
                                    //}
                                    break;
                                }

                                case EventType.WarpTarget:
                                    //AddWarpTarget(eventParams[0], x, y);
                                    break;
                                case EventType.LightReset:
                                    //eventParams[0] = (ushort)levelHandler.AmbientLightDefault;
                                    //StoreTileEvent(x, y, EventType.LightSet, eventFlags, eventParams);
                                    break;

                                default:
                                    StoreTileEvent(x, y, (EventType)eventID, eventFlags, eventParams, 0, 0);
                                    break;
                            }
                        }
                    }
                }
            }
        }

        public void ActivateEvents()
        {
            for (int i = 0; i < eventLayout.Length; i++) {
                ref EventTile tile = ref eventLayout[i];
                if (tile.IsEventActive || tile.EventType == EventType.Empty || tile.SpawnedActor != null) {
                    continue;
                }

                tile.IsEventActive = true;

                if (tile.EventType == EventType.AreaWeather) {
                    //levelHandler.ApplyWeather((LevelHandler.WeatherType)tile.EventParams[0], tile.EventParams[1], tile.EventParams[2] != 0);
                } else if (tile.EventType != EventType.Generator) {
                    int x = i % layoutWidth;
                    int y = i / layoutWidth;

                    ActorInstantiationFlags flags = ActorInstantiationFlags.IsCreatedFromEventMap | tile.EventFlags;
                    //if (allowAsync) {
                    //    flags |= ActorInstantiationFlags.Async;
                    //}

                    tile.SpawnedActor = server.EventSpawner.SpawnEvent(flags, tile.EventType, x, y, LevelHandler.MainPlaneZ, tile.EventParams);
                    if (tile.SpawnedActor != null) {
                        server.AddSpawnedActor(tile.SpawnedActor);
                    }
                }
            }
        }

        public void StoreTileEvent(int x, int y, EventType eventType, ActorInstantiationFlags eventFlags, ushort[] tileParams, float delay, float timeLeft)
        {
            if (eventType == EventType.Empty && (x < 0 || y < 0 || y >= layoutHeight || x >= layoutWidth)) {
                return;
            }

            ref EventTile previousEvent = ref eventLayout[x + y * layoutWidth];

            EventTile newEvent = new EventTile {
                EventType = eventType,
                EventFlags = eventFlags,
                EventParams = new ushort[8],
                IsEventActive = (previousEvent.EventType == eventType && previousEvent.IsEventActive),
                Delay = delay,
                TimeLeft = timeLeft
            };

            // Store event parameters
            int i = 0;
            if (tileParams != null) {
                int n = MathF.Min(tileParams.Length, 8);
                for (; i < n; ++i) {
                    newEvent.EventParams[i] = tileParams[i];
                }
            }

            previousEvent = newEvent;
        }

        public void AddSpawnPosition(int x, int y)
        {
            spawnPositions.Add(new Vector2(32 * x + 16, 32 * y + 16 - 8));
        }

        public Vector2 GetRandomSpawnPosition()
        {
            return spawnPositions[MathF.Rnd.Next(spawnPositions.Count)];
        }
    }
}

#endif