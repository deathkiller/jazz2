using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using Duality;
using Duality.Drawing;
using Duality.IO;
using Duality.Resources;
using Jazz2.Game.Structs;

namespace Jazz2.Game.Tiles
{
    public class TileSet
    {
        public static ColorRgba[] LoadPalette(string path)
        {
            if (!FileOp.Exists(path)) {
                return null;
            }

            using (Stream s = FileOp.Open(path, FileAccessMode.Read))
            using (BinaryReader r = new BinaryReader(s)) {
                int n = r.ReadUInt16();
                ColorRgba[] palette = new ColorRgba[n];
                for (int i = 0; i < n; i++) {
                    byte cR = r.ReadByte();
                    byte cG = r.ReadByte();
                    byte cB = r.ReadByte();
                    byte cA = r.ReadByte();
                    palette[i] = new ColorRgba(cR, cG, cB, cA);
                }
                return palette;
            }
        }


        public const int DefaultTileSize = 32;

        public readonly RawList<ContentRef<Material>> Materials;
        public int TileCount => defaultLayerTiles.Count;
        public readonly int TileSize;
        public readonly int TilesPerRow;
        public bool IsValid;

        private RawList<BitArray> masks;
        private BitArray isMaskEmpty;
        private BitArray isMaskFilled;

        private BitArray isTileFilled;

        private RawList<LayerTile> defaultLayerTiles;

        public TileSet(string path)
        {
            Materials = new RawList<ContentRef<Material>>();

            ContentRef<Material> material;
            PixelData mask;
            ContentResolver.Current.RequestTileset(path, out material, out mask);

            Materials.Add(material);

            TileSize = DefaultTileSize;

            PixelData texture = material.Res.MainTexture.Res.BasePixmap.Res.MainLayer;

            int width = (texture.Width / TileSize);
            int height = (texture.Height / TileSize);
            TilesPerRow = width;
            int tileCount = width * height;

            masks = new RawList<BitArray>();
            isMaskEmpty = new BitArray(tileCount);
            isMaskFilled = new BitArray(tileCount);

            isTileFilled = new BitArray(tileCount);

            defaultLayerTiles = new RawList<LayerTile>();

            for (int i = 0; i < height; i++) {
                for (int j = 0; j < width; j++) {
                    BitArray tileMask = new BitArray(TileSize * TileSize);
                    bool maskEmpty = true;
                    bool maskFilled = true;
                    bool tileFilled = true;
                    for (int x = 0; x < TileSize; x++) {
                        for (int y = 0; y < TileSize; y++) {
                            ColorRgba px = mask[j * TileSize + x, i * TileSize + y];
                            // Consider any fully white or fully transparent pixel in the masks as non-solid and all others as solid
                            bool masked = (px != ColorRgba.White && px.A > 0);
                            tileMask[x + TileSize * y] = masked;
                            maskEmpty &= !masked;
                            maskFilled &= masked;

                            ColorRgba pxTex = texture[j * TileSize + x, i * TileSize + y];
                            masked = (pxTex.A > 20);
                            tileFilled &= masked;
                        }
                    }

                    masks.Add(tileMask);

                    int idx = (j + width * i);
                    isMaskEmpty[idx] = maskEmpty;
                    isMaskFilled[idx] = maskFilled;

                    isTileFilled[idx] = tileFilled || !maskEmpty;

                    defaultLayerTiles.Add(new LayerTile {
                        TileID = idx,

                        Material = material,
                        MaterialOffset = new Point2(TileSize * ((i * width + j) % TilesPerRow), TileSize * ((i * width + j) / TilesPerRow)),
                        MaterialAlpha = 255
                    });
                }
            }

            IsValid = true;
        }

        public void MergeTiles(string path, int offset, int count)
        {
            ContentRef<Material> material;
            PixelData mask;
            ContentResolver.Current.RequestTileset(path, out material, out mask);

            Materials.Add(material);

            PixelData texture = material.Res.MainTexture.Res.BasePixmap.Res.MainLayer;

            int width = (texture.Width / TileSize);
            int height = (texture.Height / TileSize);

            isMaskEmpty.Length += count;
            isMaskFilled.Length += count;
            isTileFilled.Length += count;

            int partStart = defaultLayerTiles.Count;

            for (int i = 0; i < count; i++) {
                int ty = (offset + i) / width;
                int tx = (offset + i) % width;

                BitArray tileMask = new BitArray(TileSize * TileSize);
                bool maskEmpty = true;
                bool maskFilled = true;
                bool tileFilled = true;
                for (int x = 0; x < TileSize; x++) {
                    for (int y = 0; y < TileSize; y++) {
                        ColorRgba px = mask[tx * TileSize + x, ty * TileSize + y];
                        // Consider any fully white or fully transparent pixel in the masks as non-solid and all others as solid
                        bool masked = (px != ColorRgba.White && px.A > 0);
                        tileMask[x + TileSize * y] = masked;
                        maskEmpty &= !masked;
                        maskFilled &= masked;

                        ColorRgba pxTex = texture[tx * TileSize + x, ty * TileSize + y];
                        masked = (pxTex.A > 20);
                        tileFilled &= masked;
                    }
                }

                masks.Add(tileMask);

                int idx = (partStart + i);
                isMaskEmpty[idx] = maskEmpty;
                isMaskFilled[idx] = maskFilled;

                isTileFilled[idx] = tileFilled || !maskEmpty;

                defaultLayerTiles.Add(new LayerTile {
                    TileID = idx,

                    Material = material,
                    MaterialOffset = new Point2(tx * TileSize, ty * TileSize),
                    MaterialAlpha = 255
                });
            }

            IsValid = true;
        }

        public BitArray GetTileMask(int tileID)
        {
            if (tileID < TileCount) {
                return masks.Data[tileID];
            }
            return new BitArray(TileSize * TileSize);
        }

        public LayerTile GetDefaultTile(int tileID)
        {
            if (tileID < TileCount) {
                return defaultLayerTiles.Data[tileID];
            }
            return defaultLayerTiles.Data[0];
        }

        public Point2 GetTileTextureRect(int tileID)
        {
            return defaultLayerTiles[tileID].MaterialOffset;
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public bool IsTileMaskEmpty(int tileID)
        {
            return isMaskEmpty[tileID];
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public bool IsTileMaskFilled(int tileID)
        {
            return isMaskFilled[tileID];
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public bool IsTileFilled(int tileID)
        {
            return isTileFilled[tileID];
        }
    }
}