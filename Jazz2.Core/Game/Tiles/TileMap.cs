using System.Collections;
using System.IO;
using Duality;
using Duality.Drawing;
using Jazz2.Game.Collisions;
using Jazz2.Game.Structs;

namespace Jazz2.Game.Tiles
{
    public partial class TileMap : Component, ICmpUpdatable
    {
        private const int TriggerCount = 32;

        private ILevelHandler levelHandler;

        private TileSet tileset;

        private RawList<TileMapLayer> layers = new RawList<TileMapLayer>();
        private RawList<AnimatedTile> animatedTiles;
        private RawList<Point2> activeCollapsingTiles = new RawList<Point2>();

        private int levelWidth, levelHeight, sprLayerIndex;
        private bool hasPit;
        private int limitLeft, limitRight;

        private BitArray triggerState;

        public Point2 Size => new Point2(levelWidth, levelHeight);
        public TileSet Tileset => tileset;
        public RawList<TileMapLayer> Layers => layers;
        public int SpriteLayerIndex => sprLayerIndex;

        public TileMap(ILevelHandler levelHandler, string tilesetPath, ColorRgba[] tileMapPalette, bool hasPit)
        {
            this.levelHandler = levelHandler;
            this.hasPit = hasPit;

            tileset = new TileSet(tilesetPath, true, tileMapPalette);

            if (!tileset.IsValid) {
                throw new InvalidDataException("Tileset is corrupted");
            }

            triggerState = new BitArray(TriggerCount);
        }

        public void ReleaseResources()
        {
            if (tileset != null) {
                tileset.Dispose();
                tileset = null;
            }

            layers = null;
            animatedTiles = null;
            activeCollapsingTiles = null;

            debrisList = null;

            levelHandler = null;
        }

        void ICmpUpdatable.OnUpdate()
        {
            float timeMult = Time.TimeMult;

            int n = animatedTiles.Count;
            AnimatedTile[] list = animatedTiles.Data;
            for (int i = 0; i < n; i++) {
                list[i].UpdateTile(timeMult);
            }

            AdvanceCollapsingTileTimers();

            UpdateDebris(timeMult);
        }

        internal void ReadLayerConfiguration(LayerType type, Stream s, LevelHandler.LevelConfigJson.LayerSection layer)
        {
            using (BinaryReader r = new BinaryReader(s)) {
                int width = r.ReadInt32();
                int height = r.ReadInt32();

                TileMapLayer newLayer = new TileMapLayer {
                    Visible = true,
                    Layout = new LayerTile[width * height]
                };

                for (int i = 0; i < newLayer.Layout.Length; i++) {
                    ushort tileType = r.ReadUInt16();

                    byte flags = r.ReadByte();
                    if (flags == 0) {
                        newLayer.Layout[i] = tileset.GetDefaultTile(tileType);
                        continue;
                    }

                    bool isFlippedX = (flags & 0x01) != 0;
                    bool isFlippedY = (flags & 0x02) != 0;
                    bool isAnimated = (flags & 0x04) != 0;
                    byte tileModifier = (byte)(flags >> 4);

                    LayerTile tile;

                    // Copy the default tile and do stuff with it
                    if (!isAnimated) {
                        tile = tileset.GetDefaultTile(tileType);
                    } else {
                        // Copy the template for isAnimated tiles from the first tile, then fix the tile ID.
                        // Cannot rely on copying the same tile as its own isAnimated tile ID, because it is
                        // possible that there are more isAnimated tiles than regular ones.
                        tile = tileset.GetDefaultTile(0);
                        tile.TileID = tileType;
                    }

                    tile.IsFlippedX = isFlippedX;
                    tile.IsFlippedY = isFlippedY;
                    tile.IsAnimated = isAnimated;

                    if (tileModifier == 1 /*Translucent*/) {
                        tile.MaterialAlpha = /*127*/140;
                    } else if (tileModifier == 2 /*Invisible*/) {
                        tile.MaterialAlpha = 0;
                    }

                    newLayer.Layout[i] = tile;
                }

                if (type == LayerType.Sprite) {
                    levelWidth = width;
                    levelHeight = height;
                    sprLayerIndex = layers.Count;

                    // No limit
                    limitRight = levelWidth;
                }

                newLayer.LayoutWidth = width;

                newLayer.SpeedX = layer.XSpeed;
                newLayer.SpeedY = layer.YSpeed;
                newLayer.AutoSpeedX = layer.XAutoSpeed;
                newLayer.AutoSpeedY = layer.YAutoSpeed;
                newLayer.RepeatX = layer.XRepeat;
                newLayer.RepeatY = layer.YRepeat;
                newLayer.OffsetX = layer.XOffset;
                newLayer.OffsetY = layer.YOffset;
                newLayer.UseInherentOffset = layer.InherentOffset;
                newLayer.Depth = LevelHandler.MainPlaneZ + layer.Depth;

                newLayer.BackgroundStyle = (BackgroundStyle)layer.BackgroundStyle;
                newLayer.ParallaxStarsEnabled = layer.ParallaxStarsEnabled;
                if (layer.BackgroundColor != null && layer.BackgroundColor.Count >= 3) {
                    newLayer.BackgroundColor = new Vector4(layer.BackgroundColor[0] / 255f, layer.BackgroundColor[1] / 255f, layer.BackgroundColor[2] / 255f, 1f);
                } else {
                    newLayer.BackgroundColor = new Vector4(0, 0, 0, 1);
                }

                layers.Add(newLayer);
            }
        }

        internal void ReadAnimatedTiles(Stream s)
        {
            using (BinaryReader r = new BinaryReader(s)) {
                int count = r.ReadInt32();

                animatedTiles = new RawList<AnimatedTile>(count);

                for (int i = 0; i < count; i++) {
                    ushort frameCount = r.ReadUInt16();
                    if (frameCount == 0) {
                        continue;
                    }

                    ushort[] frames = new ushort[frameCount];
                    byte[] flags = new byte[frameCount];

                    for (int j = 0; j < frameCount; j++) {
                        frames[j] = r.ReadUInt16();
                        flags[j] = r.ReadByte();
                    }

                    byte speed = r.ReadByte();
                    ushort delay = r.ReadUInt16();
                    ushort delayJitter = r.ReadUInt16();
                    byte pingPong = r.ReadByte();
                    ushort pingPongDelay = r.ReadUInt16();

                    // ToDo: Adjust FPS in Import
                    speed = (byte)(speed * 14 / 10);

                    animatedTiles.Add(new AnimatedTile(tileset, frames, flags, speed,
                        delay, delayJitter, (pingPong > 0), pingPongDelay));
                }
            }
        }

        internal void ReadTilesetPart(string path, int offset, int count)
        {
            tileset.MergeTiles(path, offset, count);
        }

        public void SetTileEventFlags(int x, int y, EventType tileEvent, ushort[] tileParams)
        {
            ref LayerTile tile = ref layers[sprLayerIndex].Layout[x + y * levelWidth];

            switch (tileEvent) {
                case EventType.ModifierOneWay:
                    tile.IsOneWay = true;
                    break;
                case EventType.ModifierVine:
                    tile.SuspendType = SuspendType.Vine;
                    break;
                case EventType.ModifierHook:
                    tile.SuspendType = SuspendType.Hook;
                    break;
                case EventType.SceneryDestruct:
                    SetTileDestructibleEventFlag(ref tile, TileDestructType.Weapon, tileParams[0]);
                    break;
                case EventType.SceneryDestructButtstomp:
                    SetTileDestructibleEventFlag(ref tile, TileDestructType.Special, tileParams[0]);
                    break;
                case EventType.TriggerArea:
                    SetTileDestructibleEventFlag(ref tile, TileDestructType.Trigger, tileParams[0]);
                    break;
                case EventType.SceneryDestructSpeed:
                    SetTileDestructibleEventFlag(ref tile, TileDestructType.Speed, tileParams[0]);
                    break;
                case EventType.SceneryCollapse:
                    // ToDo: FPS (tileParams[1]) not used...
                    SetTileDestructibleEventFlag(ref tile, TileDestructType.Collapse, tileParams[0]);
                    break;
            }
        }

        private void SetTileDestructibleEventFlag(ref LayerTile tile, TileDestructType type, ushort extraData)
        {
            if (!tile.IsAnimated) {
                return;
            }

            tile.DestructType = type;
            tile.IsAnimated = false;
            tile.DestructAnimation = tile.TileID;
            tile.TileID = animatedTiles[tile.DestructAnimation].Tiles[0].TileID;
            tile.DestructFrameIndex = 0;
            tile.MaterialOffset = tileset.GetTileTextureRect(tile.TileID);
            tile.ExtraData = extraData;
        }

        public bool IsTileEmpty(int x, int y)
        {
            // ToDo: Is this function used correctly?
            // Consider out-of-level coordinates as solid walls
            if (x < limitLeft || y < 0 || x >= limitRight) {
                return false;
            }
            if (y >= levelHeight) {
                return hasPit;
            }

            ref LayerTile tile = ref layers[sprLayerIndex].Layout[y * levelWidth + x];

            int idx = tile.TileID;
            if (tile.IsAnimated) {
                idx = animatedTiles[idx].CurrentTile.TileID;
            }

            return tileset.IsTileMaskEmpty(idx);
        }

        public bool IsTileEmpty(ref AABB aabb, bool downwards)
        {
            int limitLeftPx = limitLeft << 5;
            int limitRightPx = limitRight << 5;
            int limitBottomPx = levelHeight << 5;

            // Consider out-of-level coordinates as solid walls
            if (aabb.LowerBound.X < limitLeftPx || aabb.LowerBound.Y < 0 || aabb.UpperBound.X >= limitRightPx) {
                return false;
            }
            if (aabb.UpperBound.Y >= limitBottomPx) {
                return hasPit;
            }

            // Check all covered tiles for collisions; if all are empty, no need to do pixel collision checking
            int hx1 = MathF.Max((int)aabb.LowerBound.X, limitLeftPx);
            int hx2 = MathF.Min((int)MathF.Ceiling(aabb.UpperBound.X), limitRightPx - 1);
            int hy1 = (int)aabb.LowerBound.Y;
            int hy2 = MathF.Min((int)MathF.Ceiling(aabb.UpperBound.Y), limitBottomPx - 1);

            int hx1t = hx1 >> 5;
            int hx2t = hx2 >> 5;
            int hy1t = hy1 >> 5;
            int hy2t = hy2 >> 5;

            LayerTile[] sprLayerLayout = layers.Data[sprLayerIndex].Layout;

            for (int y = hy1t; y <= hy2t; y++) {
                for (int x = hx1t; x <= hx2t; x++) {
                    ref LayerTile tile = ref sprLayerLayout[y * levelWidth + x];

                    int idx = tile.TileID;
                    if (tile.IsAnimated) {
                        idx = animatedTiles[idx].CurrentTile.TileID;
                    }

                    if (tile.SuspendType != SuspendType.None || tileset.IsTileMaskEmpty(idx) || (tile.IsOneWay && !downwards)) {
                        continue;
                    }

                    int tx = x << 5;
                    int ty = y << 5;

                    int left = MathF.Max(hx1 - tx, 0);
                    int right = MathF.Min(hx2 - tx, 31);
                    int top = MathF.Max(hy1 - ty, 0);
                    int bottom = MathF.Min(hy2 - ty, 31);

                    if (tile.IsFlippedX) {
                        int left2 = left;
                        left = (31 - right);
                        right = (31 - left2);
                    }
                    if (tile.IsFlippedY) {
                        int top2 = top;
                        top = (31 - bottom);
                        bottom = (31 - top2);
                    }

                    top <<= 5;
                    bottom <<= 5;

                    BitArray mask = tileset.GetTileMask(idx);
                    for (int ry = top; ry <= bottom; ry += 32) {
                        for (int rx = left; rx <= right; rx++) {
                            if (mask[ry | rx]) {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        public SuspendType GetTileSuspendState(float x, float y)
        {
            const int Tolerance = 4;

            if (x < 0 || y < 0) {
                return SuspendType.None;
            }

            int ax = (int)x >> 5;
            int ay = (int)y >> 5;

            if (ax >= levelWidth || ay >= levelHeight) {
                return SuspendType.None;
            }

            ref LayerTile tile = ref layers[sprLayerIndex].Layout[ax + ay * levelWidth];
            if (tile.SuspendType == SuspendType.None) {
                return SuspendType.None;
            }

            BitArray mask;
            if (tile.IsAnimated) {
                if (tile.TileID < animatedTiles.Count) {
                    mask = tileset.GetTileMask(animatedTiles[tile.TileID].CurrentTile.TileID);
                } else {
                    return SuspendType.None;
                }
            } else {
                mask = tileset.GetTileMask(tile.TileID);
            }

            int rx = (int)x & 31;
            int ry = (int)y & 31;

            if (tile.IsFlippedX) {
                rx = (31 - rx);
            }
            if (tile.IsFlippedY) {
                ry = (31 - ry);
            }

            int top = MathF.Max(ry - Tolerance, 0) << 5;
            int bottom = MathF.Min(ry + Tolerance, 31) << 5;

            for (int ti = bottom | rx; ti >= top; ti -= 32) {
                if (mask[ti]) {
                    return tile.SuspendType;
                }
            }

            return SuspendType.None;
        }

        public void SetSolidLimit(int tileLeft, int tileWidth)
        {
            if (tileLeft <= 0) {
                limitLeft = 0;
            } else {
                limitLeft = tileLeft;
            }
            
            if (tileWidth > 0) {
                limitRight = tileLeft + tileWidth;
            } else {
                limitRight = levelWidth;
            }
        }
    }
}