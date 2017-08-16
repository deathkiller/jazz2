using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Import;

namespace Jazz2.Compatibility
{
    public class JJ2Tileset // .j2t
    {
        private class TilesetTileSection
        {
            public bool opaque;
            public uint imageDataOffset;
            public uint alphaDataOffset;
            public uint maskDataOffset;
            public Bitmap image;
            public Bitmap mask;
        }

        private string name;
        private JJ2Version version;
        private Color[] palette;
        private TilesetTileSection[] tiles;
        private int tileCount;

        public string Name => name;

        public ushort MaxSupportedTiles => (ushort)(version == JJ2Version.BaseGame ? 1024 : 4096);

        public static JJ2Tileset Open(string path, bool strictParser)
        {
            using (Stream s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                s.Seek(180, SeekOrigin.Current);

                JJ2Tileset tileset = new JJ2Tileset();

                JJ2Block headerBlock = new JJ2Block(s, 262 - 180);

                // Read the next four bytes; should spell out "LEVL"
                uint id = headerBlock.ReadUInt32();
                if (id != 0x454C4954) {
                    throw new InvalidOperationException("Invalid magic number");
                }

                uint hash = headerBlock.ReadUInt32();
                if (hash != 0xAFBEADDE) {
                    throw new InvalidOperationException("Invalid magic number");
                }

                tileset.name = headerBlock.ReadString(32, true);

                ushort versionNum = headerBlock.ReadUInt16();
                tileset.version = (versionNum <= 512 ? JJ2Version.BaseGame : JJ2Version.TSF);

                int recordedSize = headerBlock.ReadInt32();
                if (strictParser && s.Length != recordedSize) {
                    throw new InvalidOperationException("Unexpected file size");
                }

                // Get the CRC; would check here if it matches if we knew what variant it is AND what it applies to
                // Test file across all CRC32 variants + Adler had no matches to the value obtained from the file
                // so either the variant is something else or the CRC is not applied to the whole file but on a part
                int recordedCRC = headerBlock.ReadInt32();

                // Read the lengths, uncompress the blocks and bail if any block could not be uncompressed
                // This could look better without all the copy-paste, but meh.
                int infoBlockPackedSize, imageBlockPackedSize, alphaBlockPackedSize, maskBlockPackedSize,
                        infoBlockUnpackedSize, imageBlockUnpackedSize, alphaBlockUnpackedSize, maskBlockUnpackedSize;
                infoBlockPackedSize = headerBlock.ReadInt32();
                infoBlockUnpackedSize = headerBlock.ReadInt32();
                imageBlockPackedSize = headerBlock.ReadInt32();
                imageBlockUnpackedSize = headerBlock.ReadInt32();
                alphaBlockPackedSize = headerBlock.ReadInt32();
                alphaBlockUnpackedSize = headerBlock.ReadInt32();
                maskBlockPackedSize = headerBlock.ReadInt32();
                maskBlockUnpackedSize = headerBlock.ReadInt32();

                JJ2Block infoBlock = new JJ2Block(s, infoBlockPackedSize, infoBlockUnpackedSize);
                JJ2Block imageBlock = new JJ2Block(s, imageBlockPackedSize, imageBlockUnpackedSize);
                JJ2Block alphaBlock = new JJ2Block(s, alphaBlockPackedSize, alphaBlockUnpackedSize);
                JJ2Block maskBlock = new JJ2Block(s, maskBlockPackedSize, maskBlockUnpackedSize);

                tileset.LoadMetadata(infoBlock);
                tileset.LoadImageData(imageBlock, alphaBlock);
                tileset.LoadMaskData(maskBlock);

                return tileset;
            }
        }

        private void LoadMetadata(JJ2Block block)
        {
            palette = new Color[256];

            for (int i = 0; i < 256; i++) {
                byte red = block.ReadByte();
                byte green = block.ReadByte();
                byte blue = block.ReadByte();
                byte alpha = block.ReadByte();
                palette[i] = Color.FromArgb(255 - alpha, red, green, blue);
            }

            tileCount = block.ReadInt32();

            int maxTiles = MaxSupportedTiles;
            tiles = new TilesetTileSection[maxTiles];

            for (int i = 0; i < maxTiles; ++i) {
                bool opaque = block.ReadBool();

                TilesetTileSection tile = new TilesetTileSection();
                tile.opaque = opaque;
                tiles[i] = tile;
            }

            // block of unknown values, skip
            block.DiscardBytes(maxTiles);

            for (int i = 0; i < maxTiles; ++i) {
                tiles[i].imageDataOffset = block.ReadUInt32();
            }

            // block of unknown values, skip
            block.DiscardBytes(4 * maxTiles);

            for (int i = 0; i < maxTiles; ++i) {
                tiles[i].alphaDataOffset = block.ReadUInt32();
            }

            // block of unknown values, skip
            block.DiscardBytes(4 * maxTiles);

            for (int i = 0; i < maxTiles; ++i) {
                tiles[i].maskDataOffset = block.ReadUInt32();
            }

            // we don't care about the flipped masks, those are generated on runtime
            block.DiscardBytes(4 * maxTiles);
        }

        private void LoadImageData(JJ2Block imageBlock, JJ2Block alphaBlock)
        {
            const int BlockSize = 32;

            for (int i = 0; i < tiles.Length; i++) {
                tiles[i].image = new Bitmap(BlockSize, BlockSize);

                byte[] imageData = imageBlock.ReadRawBytes(BlockSize * BlockSize, tiles[i].imageDataOffset);
                byte[] alphaMaskData = alphaBlock.ReadRawBytes(128, tiles[i].alphaDataOffset);
                for (int j = 0; j < (BlockSize * BlockSize); j++) {
                    byte idx = imageData[j];
                    Color color;
                    if (alphaMaskData.Length > 0 && ((alphaMaskData[j / 8] >> (j % 8)) & 0x01) == 0x00) {
                        color = Color.Transparent;
                    } else {
                        color = palette[idx];
                    }

                    tiles[i].image.SetPixel(j % 32, j / 32, color);
                }
            }
        }

        private void LoadMaskData(JJ2Block block)
        {
            const int BlockSize = 32;

            for (int i = 0; i < tiles.Length; i++) {
                tiles[i].mask = new Bitmap(BlockSize, BlockSize);

                byte[] maskData = block.ReadRawBytes(128, tiles[i].maskDataOffset);
                for (int j = 0; j < 128; j++) {
                    byte idx = maskData[j];
                    for (int k = 0; k < 8; k++) {
                        int pixelIdx = 8 * j + k;
                        if (((idx >> k) & 0x01) == 0) {
                            tiles[i].mask.SetPixel(pixelIdx % 32, pixelIdx / 32, Color.Transparent);
                        } else {
                            tiles[i].mask.SetPixel(pixelIdx % 32, pixelIdx / 32, Color.Black);
                        }
                    }
                }
            }
        }

        public void Convert(string path)
        {
            const int TileSize = 32;
            // Rearrange tiles from '10 tiles per row' to '30 tiles per row'
            const int TilesPerRow = 30;

            // Save tiles and mask
            Bitmap tilesTexture = new Bitmap(TileSize * TilesPerRow, ((tileCount - 1) / TilesPerRow + 1) * TileSize);
            Bitmap masksTexture = new Bitmap(TileSize * TilesPerRow, ((tileCount - 1) / TilesPerRow + 1) * TileSize);

            using (Graphics tilesTextureG = Graphics.FromImage(tilesTexture))
            using (Graphics masksTextureG = Graphics.FromImage(masksTexture)) {
                tilesTextureG.Clear(Color.Transparent);
                masksTextureG.Clear(Color.Transparent);

                int maxTiles = MaxSupportedTiles;
                for (int i = 0; i < maxTiles; i++) {
                    tilesTextureG.DrawImage(tiles[i].image, (i % TilesPerRow) * TileSize, (i / TilesPerRow) * TileSize);
                    masksTextureG.DrawImage(tiles[i].mask, (i % TilesPerRow) * TileSize, (i / TilesPerRow) * TileSize);
                }
            }

            tilesTexture.Save(Path.Combine(path, "tiles.png"), ImageFormat.Png);
            masksTexture.Save(Path.Combine(path, "mask.png"), ImageFormat.Png);

            // Create normal map
            using (Bitmap normalMap = NormalMapGenerator.FromSprite(tilesTexture,
                    new Point(tilesTexture.Width / TileSize, tilesTexture.Height / TileSize),
                    false)) {

                normalMap.Save(Path.Combine(path, "normal.png"), ImageFormat.Png);
            }

            // Save tileset palette
            if (palette != null && palette.Length > 1) {
                using (FileStream s = File.Open(Path.Combine(path, ".palette"), FileMode.Create, FileAccess.Write))
                using (BinaryWriter w = new BinaryWriter(s)) {
                    w.Write((ushort)palette.Length);
                    w.Write((int)0); // Empty color
                    for (int i = 1; i < palette.Length; i++) {
                        w.Write((byte)palette[i].R);
                        w.Write((byte)palette[i].G);
                        w.Write((byte)palette[i].B);
                        w.Write((byte)palette[i].A);
                    }
                }
            }
        }
    }
}