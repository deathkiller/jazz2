using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Duality;
using Jazz2.Actors;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;

namespace Jazz2.Game.Events
{
    public class EventMap : Disposable
    {
        private struct EventTile
        {
            public EventType EventType;
            public ActorInstantiationFlags EventFlags;
            public ushort[] EventParams;
            public bool IsEventActive;
        }

        private struct GeneratorInfo
        {
            public int EventPos;

            public EventType EventType;
            public ushort[] EventParams;
            public byte Delay;
            public float TimeLeft;

            public ActorBase SpawnedActor;
        }

        private LevelHandler levelHandler;

        private EventTile[] eventLayout;
        private EventTile[] eventLayoutForRollback;
        private int layoutWidth, layoutHeight;

        private Dictionary<uint, List<Vector2>> warpTargets;
        private Dictionary<PlayerType, List<Vector2>> spawnPositions;

        private RawList<GeneratorInfo> generators = new RawList<GeneratorInfo>();

        public EventMap(LevelHandler levelHandler, Point2 size)
        {
            this.levelHandler = levelHandler;

            layoutWidth = size.X;
            layoutHeight = size.Y;

            eventLayout = new EventTile[size.X * size.Y];
            eventLayoutForRollback = new EventTile[size.X * size.Y];

            warpTargets = new Dictionary<uint, List<Vector2>>();
            spawnPositions = new Dictionary<PlayerType, List<Vector2>>();
        }

        protected override void Dispose(bool disposing)
        {
            levelHandler = null;
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public bool HasEventByPosition(int x, int y)
        {
            return (x >= 0 && y >= 0 && y < layoutHeight && x < layoutWidth && eventLayout[x + y * layoutWidth].EventType != EventType.Empty);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public EventType GetEventByPosition(float x, float y, ref ushort[] eventParams)
        {
            return GetEventByPosition((int)x / 32, (int)y / 32, ref eventParams);
        }

        public EventType GetEventByPosition(int x, int y, ref ushort[] eventParams)
        {
            if (y > layoutHeight) {
                return EventType.ModifierDeath;
            }

            if (HasEventByPosition(x, y)) {
                eventParams = eventLayout[x + y * layoutWidth].EventParams;
                return eventLayout[x + y * layoutWidth].EventType;
            }
            return EventType.Empty;
        }

        public bool IsHurting(float x, float y)
        {
            // ToDo: Implement all JJ2+ parameters (directional hurt events)
            int tx = (int)x / 32;
            int ty = (int)y / 32;

            ushort[] eventParams = null;
            if (GetEventByPosition(tx, ty, ref eventParams) != EventType.ModifierHurt) {
                return false;
            }

            return !levelHandler.TileMap.IsTileEmpty(tx, ty);
        }

        public int IsPole(float x, float y)
        {
            ushort[] eventParams = null;
            EventType e = GetEventByPosition((int)x / 32, (int)y / 32, ref eventParams);
            return (e == EventType.ModifierHPole ? 2 : (e == EventType.ModifierVPole ? 1 : 0));
        }

        public int GetWarpByPosition(float x, float y)
        {
            int tx = (int)x / 32;
            int ty = (int)y / 32;
            ushort[] eventParams = null;
            if (GetEventByPosition(tx, ty, ref eventParams) == EventType.WarpOrigin) {
                return eventParams[0];
            } else {
                return -1;
            }
        }

        public Vector2 GetWarpTarget(uint id)
        {
            List<Vector2> targets;
            if (!warpTargets.TryGetValue(id, out targets) || targets.Count == 0) {
                return new Vector2(-1, -1);
            }

            return targets[MathF.Rnd.Next(targets.Count)];
        }

        private void AddWarpTarget(uint id, int x, int y)
        {
            List<Vector2> targets;
            if (!warpTargets.TryGetValue(id, out targets)) {
                targets = new List<Vector2>();
                warpTargets[id] = targets;
            }

            targets.Add(new Vector2(x * 32 + 16, y * 32 + 12));
        }

        public Vector2 GetSpawnPosition(PlayerType type)
        {
            List<Vector2> targets;
            if (!spawnPositions.TryGetValue(type, out targets) || targets.Count == 0) {
                return new Vector2(-1, -1);
            }

            return targets[MathF.Rnd.Next(targets.Count)];
        }

        private void AddSpawnPosition(PlayerType type, int x, int y)
        {
            List<Vector2> targets;
            if (!spawnPositions.TryGetValue(type, out targets)) {
                targets = new List<Vector2>();
                spawnPositions[type] = targets;
            }

            targets.Add(new Vector2(32 * x + 16, 32 * y + 16 - 8));
        }

        public void ReadEvents(Stream s, uint layoutVersion, GameDifficulty difficulty)
        {
            using (BinaryReader r = new BinaryReader(s)) {
                int width = r.ReadInt32();
                int height = r.ReadInt32();

                byte difficultyBit;
                switch (difficulty) {
                    case GameDifficulty.Easy:
                        difficultyBit = 4;
                        break;
                    case GameDifficulty.Hard:
                        difficultyBit = 6;
                        break;
                    case GameDifficulty.Normal:
                    //case GameDifficulty.Default:
                    //case GameDifficulty.Multiplayer:
                    default:
                        difficultyBit = 5;
                        break;
                }

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
                            if ((flags & (0x01 << difficultyBit)) != 0 && (flags & 0x80) == 0) {
                                ushort generatorIdx = (ushort)generators.Count;
                                float timeLeft = ((generatorFlags & 0x01) != 0 ? generatorDelay : 0f);

                                generators.Add(new GeneratorInfo {
                                    EventPos = x + y * layoutWidth,
                                    EventType = (EventType)eventID,
                                    EventParams = eventParams,
                                    Delay = generatorDelay,
                                    TimeLeft = timeLeft
                                });

                                StoreTileEvent(x, y, EventType.Generator, eventFlags, new[] { generatorIdx });
                            }
                            continue;
                        }

                        // If the difficulty bytes for the event don't match the selected difficulty, don't add anything to the event map
                        // Additionally, never show events that are multiplayer-only
                        if (flags == 0 || ((flags & (0x01 << difficultyBit)) != 0 && (flags & 0x80) == 0)) {
                            switch ((EventType)eventID) {
                                case EventType.Empty:
                                    break;

                                case EventType.LevelStart: {
                                    for (int i = 0; i < /*16*/4; i++) {
                                        if ((eventParams[0] & (1 << i)) != 0) { // Bitmask - 1: Jazz, 2: Spaz, 4: Lori
                                            AddSpawnPosition((PlayerType)i, x, y);
                                        }
                                    }
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
                                    StoreTileEvent(x, y, (EventType)eventID, eventFlags, eventParams);
                                    TileMap tiles = levelHandler.TileMap;
                                    if (tiles != null) {
                                        tiles.SetTileEventFlags(x, y, (EventType)eventID, eventParams);
                                    }
                                    break;
                                }

                                case EventType.WarpTarget:
                                    AddWarpTarget(eventParams[0], x, y);
                                    break;
                                case EventType.LightReset:
                                    eventParams[0] = (ushort)levelHandler.AmbientLightDefault;
                                    StoreTileEvent(x, y, EventType.LightSet, eventFlags, eventParams);
                                    break;

                                default:
                                    StoreTileEvent(x, y, (EventType)eventID, eventFlags, eventParams);
                                    break;
                            }
                        }
                    }
                }
            }

            Array.Copy(eventLayout, eventLayoutForRollback, eventLayout.Length);
        }

        public void StoreTileEvent(int x, int y, EventType eventType, ActorInstantiationFlags eventFlags = ActorInstantiationFlags.None, ushort[] tileParams = null)
        {
            if (eventType == EventType.Empty && (x < 0 || y < 0 || y >= layoutHeight || x >= layoutWidth)) {
                return;
            }

            ref EventTile previousEvent = ref eventLayout[x + y * layoutWidth];

            EventTile newEvent = new EventTile {
                EventType = eventType,
                EventFlags = eventFlags,
                EventParams = new ushort[8],
                IsEventActive = (previousEvent.EventType == eventType && previousEvent.IsEventActive)
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

        public void ProcessGenerators()
        {
            for (int i = 0; i < generators.Count; i++) {
                ref GeneratorInfo generator = ref generators.Data[i];

                if (!eventLayout[generator.EventPos].IsEventActive) {
                    // Generator is inactive (and recharging)
                    generator.TimeLeft -= Time.TimeMult;
                } else if (generator.SpawnedActor == null || generator.SpawnedActor.Scene == null) {
                    if (generator.TimeLeft <= 0f) {
                        // Generator is active and is ready to spawn new actor
                        generator.TimeLeft = generator.Delay * Time.FramesPerSecond;

                        int x = generator.EventPos % layoutWidth;
                        int y = generator.EventPos / layoutWidth;

                        ActorBase actor = levelHandler.EventSpawner.SpawnEvent(ActorInstantiationFlags.IsFromGenerator,
                            generator.EventType, x, y, LevelHandler.MainPlaneZ, generator.EventParams);
                        if (actor != null) {
                            levelHandler.AddActor(actor);
                            generator.SpawnedActor = actor;
                        }
                    } else {
                        // Generator is active and recharging
                        generator.TimeLeft -= Time.TimeMult;
                    }
                }
            }
        }

        public void ActivateEvents(int tx1, int ty1, int tx2, int ty2, bool allowAsync)
        {
            TileMap tiles = levelHandler.TileMap;
            if (tiles == null) {
                return;
            }

            Point2 levelSize = tiles.Size;

            int x1 = MathF.Max(0, tx1);
            int x2 = MathF.Min(levelSize.X - 1, tx2);
            int y1 = MathF.Max(0, ty1);
            int y2 = MathF.Min(levelSize.Y - 1, ty2);

            for (int x = x1; x <= x2; x++) {
                for (int y = y1; y <= y2; y++) {
                    int tileID = x + y * layoutWidth;
                    ref EventTile tile = ref eventLayout[tileID];

                    if (!tile.IsEventActive && tile.EventType != EventType.Empty) {
                        tile.IsEventActive = true;

                        if (tile.EventType == EventType.Weather) {
                            levelHandler.ApplyWeather((LevelHandler.WeatherType)tile.EventParams[0], tile.EventParams[1], tile.EventParams[2] != 0);
                        } else if (tile.EventType != EventType.Generator) {
                            ActorInstantiationFlags flags = ActorInstantiationFlags.IsCreatedFromEventMap | tile.EventFlags;
                            if (allowAsync) {
                                flags |= ActorInstantiationFlags.Async;
                            }

                            ActorBase actor = levelHandler.EventSpawner.SpawnEvent(flags, tile.EventType, x, y, LevelHandler.MainPlaneZ, tile.EventParams);
                            if (actor != null) {
                                levelHandler.AddActor(actor);
                            }
                        }
                    }
                }
            }
        }

        public void Deactivate(int x, int y)
        {
            if (HasEventByPosition(x, y)) {
                eventLayout[x + y * layoutWidth].IsEventActive = false;
            }
        }

        public void DeactivateAll()
        {
            for (int i = 0; i < eventLayout.Length; ++i) {
                eventLayout[i].IsEventActive = false;
            }
        }

        public void ResetGenerator(int tx, int ty)
        {
            // Linked actor was deactivated, but not destroyed
            // Reset its generator, so it can be respawned immediately
            ushort generatorIdx = eventLayout[tx + ty * layoutWidth].EventParams[0];

            generators.Data[generatorIdx].TimeLeft = 0f;
        }

        public void CreateCheckpointForRollback()
        {
            Array.Copy(eventLayout, eventLayoutForRollback, eventLayout.Length);
        }

        public void RollbackToCheckpoint()
        {
            Array.Copy(eventLayoutForRollback, eventLayout, eventLayout.Length);
        }
    }
}