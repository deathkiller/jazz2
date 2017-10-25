using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using Duality;
using Duality.Drawing;
using Duality.IO;
using Jazz2.Game.Structs;

namespace Jazz2.Game.Tiles
{
    public partial class TileMap : Component, ICmpUpdatable
    {
        private const int TriggerCount = 32;

        private ILevelHandler levelHandler;

        private TileSet tileset;
        private RawList<TileMapLayer> levelLayout = new RawList<TileMapLayer>();
        private RawList<AnimatedTiles> animatedTiles;
        private RawList<Point2> activeCollapsingTiles = new RawList<Point2>();

        private int levelWidth, levelHeight, sprLayerIndex;
        private bool hasPit;
        private int limitLeft, limitRight;

        private BitArray triggerState;

        public Point2 Size => new Point2(levelWidth, levelHeight);
        public TileSet Tileset => tileset;

        public TileMap(ILevelHandler levelHandler, string tilesPath, string maskPath, string normalPath, bool hasPit)
        {
            this.levelHandler = levelHandler;
            this.hasPit = hasPit;

            IImageCodec codec = ImageCodec.GetRead(ImageCodec.FormatPng);

            tileset = new TileSet(
                codec.Read(FileOp.Open(tilesPath, FileAccessMode.Read)),
                codec.Read(FileOp.Open(maskPath, FileAccessMode.Read)),
                (FileOp.Exists(normalPath) ? codec.Read(FileOp.Open(normalPath, FileAccessMode.Read)) : null)
            );

            if (!tileset.IsValid) {
                throw new InvalidDataException("Tileset is corrupted");
            }

            triggerState = new BitArray(TriggerCount);
        }

        void ICmpUpdatable.OnUpdate()
        {
            float timeMult = Time.TimeMult;

            int n = animatedTiles.Count;
            AnimatedTiles[] list = animatedTiles.Data;
            for (int i = 0; i < n; i++) {
                list[i].UpdateTile(timeMult);
            }

            AdvanceCollapsingTileTimers();

            UpdateDebris(timeMult);
        }

        internal void ReadLayerConfiguration(LayerType type, int layerIdx, string path, string layerName, LevelHandler.LevelConfigJson.LayerSection layer)
        {
            using (Stream s = FileOp.Open(PathOp.Combine(path, layerName + ".layer"), FileAccessMode.Read))
            using (DeflateStream deflate = new DeflateStream(s, CompressionMode.Decompress))
            using (BinaryReader r = new BinaryReader(deflate)) {
                int width = r.ReadInt32();
                int height = r.ReadInt32();

                TileMapLayer newLayer = new TileMapLayer();
                newLayer.Index = layerIdx;
                newLayer.Layout = new LayerTile[width * height];

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
                    bool legacyTranslucent = (flags & 0x80) != 0;

                    // Invalid tile numbers (higher than tileset tile amount) are silently changed to empty tiles
                    if (tileType >= tileset.TileCount && !isAnimated) {
                        tileType = 0;
                    }

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

                    if (legacyTranslucent) {
                        tile.MaterialAlpha = /*127*/140;
                    }

                    newLayer.Layout[i] = tile;
                }

                if (type == LayerType.Sprite) {
                    levelWidth = width;
                    levelHeight = height;
                    sprLayerIndex = levelLayout.Count;

                    // No limit
                    limitRight = levelWidth;
                }

                newLayer.LayoutWidth = width;

                newLayer.SpeedX = layer.XSpeed;
                newLayer.SpeedY = layer.YSpeed;
                newLayer.RepeatX = layer.XRepeat;
                newLayer.RepeatY = layer.YRepeat;
                newLayer.AutoSpeedX = layer.XAutoSpeed;
                newLayer.AutoSpeedY = layer.YAutoSpeed;
                newLayer.UseInherentOffset = layer.InherentOffset;
                newLayer.Depth = LevelHandler.MainPlaneZ + layer.Depth;

                newLayer.BackgroundStyle = (BackgroundStyle)layer.BackgroundStyle;
                newLayer.UseStarsTextured = layer.ParallaxStarsEnabled;
                if (layer.BackgroundColor != null && layer.BackgroundColor.Count >= 3) {
                    newLayer.BackgroundColor = new ColorRgba((byte)layer.BackgroundColor[0], (byte)layer.BackgroundColor[1], (byte)layer.BackgroundColor[2]);
                } else {
                    newLayer.BackgroundColor = ColorRgba.Black;
                }

                levelLayout.Add(newLayer);
            }
        }

        internal void ReadAnimatedTiles(string filename)
        {
            using (Stream s = FileOp.Open(filename, FileAccessMode.Read))
            using (DeflateStream deflate = new DeflateStream(s, CompressionMode.Decompress))
            using (BinaryReader r = new BinaryReader(deflate)) {
                int count = r.ReadInt32();

                animatedTiles = new RawList<AnimatedTiles>(count);

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

                    animatedTiles.Add(new AnimatedTiles(tileset, frames, flags, speed,
                        delay, delayJitter, (pingPong > 0), pingPongDelay));
                }
            }
        }

        public void SetTileEventFlags(int x, int y, EventType tileEvent, ushort[] tileParams)
        {
            ref LayerTile tile = ref levelLayout[sprLayerIndex].Layout[x + y * levelWidth];

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
            tile.TileID = animatedTiles[tile.DestructAnimation][0];
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

            int idx = levelLayout[sprLayerIndex].Layout[x + y * levelWidth].TileID;
            if (levelLayout[sprLayerIndex].Layout[x + y * levelWidth].IsAnimated) {
                idx = animatedTiles[idx].CurrentTile.TileID;
            }

            if (tileset.IsTileMaskEmpty(idx)) {
                return true;
            } else {
                return false;
            }
        }

        public bool IsTileEmpty(ref Hitbox hitbox, bool downwards)
        {
            //int tileSize = Tileset.TileSize;
            const int tileSize = 32;

            int limitLeftPx = limitLeft * tileSize;
            int limitRightPx = limitRight * tileSize;
            int limitBottomPx = levelHeight * tileSize;

            // Consider out-of-level coordinates as solid walls
            if (hitbox.Left < limitLeftPx || hitbox.Top < 0 || hitbox.Right >= limitRightPx) {
                return false;
            }
            if (hitbox.Bottom >= limitBottomPx) {
                return hasPit;
            }

            // Check all covered tiles for collisions; if all are empty, no need to do pixel level collision checking
            int hx1 = (int)MathF.Max(MathF.Floor(hitbox.Left), limitLeftPx);
            int hx2 = (int)MathF.Min(MathF.Ceiling(hitbox.Right), limitRightPx - 1);
            int hy1 = (int)MathF.Floor(hitbox.Top);
            int hy2 = (int)MathF.Min(MathF.Ceiling(hitbox.Bottom), limitBottomPx - 1);

            LayerTile[] sprLayerLayout = levelLayout.Data[sprLayerIndex].Layout;

            for (int x = hx1 / tileSize; x <= hx2 / tileSize; x++) {
                for (int y = hy1 / tileSize; y <= hy2 / tileSize; y++) {
                    int idx = sprLayerLayout[x + y * levelWidth].TileID;
                    if (sprLayerLayout[x + y * levelWidth].IsAnimated) {
                        idx = animatedTiles[idx].CurrentTile.TileID;
                    }

                    if (sprLayerLayout[x + y * levelWidth].SuspendType == SuspendType.None &&
                        !tileset.IsTileMaskEmpty(idx) &&
                        !(sprLayerLayout[x + y * levelWidth].IsOneWay && !downwards)) {

                        goto NOT_EMPTY;
                    }
                }
            }

            return true;

        NOT_EMPTY:
            // Check each tile pixel perfectly for collisions
            for (int x = hx1 / tileSize; x <= hx2 / tileSize; x++) {
                for (int y = hy1 / tileSize; y <= hy2 / tileSize; y++) {
                    ref LayerTile tile = ref sprLayerLayout[x + y * levelWidth];
                    int idx = tile.TileID;
                    if (sprLayerLayout[x + y * levelWidth].IsAnimated) {
                        idx = animatedTiles[idx].CurrentTile.TileID;
                    }

                    if ((sprLayerLayout[x + y * levelWidth].IsOneWay && !downwards && hy2 < (y + 1) * tileSize) ||
                        sprLayerLayout[x + y * levelWidth].SuspendType != SuspendType.None) {
                        continue;
                    }

                    BitArray mask = tileset.GetTileMask(idx);
                    for (int i = 0; i < (tileSize * tileSize); i++) {
                        int nowx = (tileSize * x + i % tileSize);
                        int nowy = (tileSize * y + i / tileSize);
                        if (hx2 < nowx || hx1 >= nowx) {
                            continue;
                        }
                        if (hy2 < nowy || hy1 >= nowy) {
                            continue;
                        }
                        int px_idx;
                        if (tile.IsFlippedX || tile.IsFlippedY) {
                            px_idx = (tile.IsFlippedX ? (31 - (i & 31)) : (i & 31)) | (tile.IsFlippedY ? ((31 - ((i >> 5) & 31)) << 5) : (i & ~31));
                        } else {
                            px_idx = i;
                        }

                        if (mask[px_idx]) {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public SuspendType GetTileSuspendState(float x, float y)
        {
            //int ax = (int)x / levelTileset.TileSize;
            //int ay = (int)y / levelTileset.TileSize;
            //int rx = (int)x - levelTileset.TileSize * ax;
            //int ry = (int)y - levelTileset.TileSize * ay;
            int ax = (int)x >> 5;
            int ay = (int)y >> 5;
            int rx = (int)x & 31;
            int ry = (int)y & 31;

            // ToDo: negative coordinates collides with bit-shifting
            if (ax < 0 || ay < 0 || ax >= levelWidth || ay >= levelHeight) {
                return SuspendType.None;
            }

            ref LayerTile tile = ref levelLayout[sprLayerIndex].Layout[ax + ay * levelWidth];
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

            for (int ty = ry - 4; ty < ry + 4; ty++) {
                if (ty < 0 || ty > 31) {
                    continue;
                }

                int i;
                if (tile.IsFlippedX || tile.IsFlippedY) {
                    i = (tile.IsFlippedX ? (31 - rx) : rx) | ((tile.IsFlippedY ? (31 - ty) : ty) << 5);
                } else {
                    i = rx | (ty << 5);
                }

                if (mask[i]) {
                    return tile.SuspendType;
                }
            }

            return SuspendType.None;
        }

        public void SetSolidLimit(int tileLeft, int tileWidth)
        {
            limitLeft = tileLeft;
            if (tileWidth > 0) {
                limitRight = tileLeft + tileWidth;
            } else {
                limitRight = levelWidth;
            }
        }
    }
}