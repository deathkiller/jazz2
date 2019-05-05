using System.Collections.Generic;
using System.IO;
using Duality;
using Jazz2.Actors;
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
        }

        private EventTile[] eventLayout;
        private int layoutWidth, layoutHeight;

        private List<Vector2> spawnPositions = new List<Vector2>();

        public ServerEventMap(Point2 size)
        {
            layoutWidth = size.X;
            layoutHeight = size.Y;

            eventLayout = new EventTile[size.X * size.Y];

        }

        public void ReadEvents(Stream s, uint layoutVersion)
        {
            using (BinaryReader r = new BinaryReader(s)) {
                int width = r.ReadInt32();
                int height = r.ReadInt32();

                byte difficultyBit = 5;

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
                                /*ushort generatorIdx = (ushort)generators.Count;
                                float timeLeft = ((generatorFlags & 0x01) != 0 ? generatorDelay : 0f);

                                generators.Add(new GeneratorInfo {
                                    EventPos = x + y * layoutWidth,
                                    EventType = (EventType)eventID,
                                    EventParams = eventParams,
                                    Delay = generatorDelay,
                                    TimeLeft = timeLeft
                                });

                                StoreTileEvent(x, y, EventType.Generator, eventFlags, new[] { generatorIdx });*/
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
                                    //StoreTileEvent(x, y, (EventType)eventID, eventFlags, eventParams);
                                    break;
                            }
                        }
                    }
                }
            }
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
