using System;
using Duality;
using Duality.Drawing;
using Duality.Resources;

namespace Jazz2.Game
{
    partial class ContentResolver
    {
        private const int ColorsPerPalette = 256;
        private const int NumberOfPalettes = 256;

        private ContentRef<Texture> paletteTexture;
        private int lastPaletteRow;

        public ContentRef<Texture> Palette => paletteTexture;

        public void ApplyBasePalette(ColorRgba[] basePalette)
        {
            if (paletteTexture.IsExplicitNull) {
                // Palette Texture is not created yet
                PixelData pixels = new PixelData(ColorsPerPalette, NumberOfPalettes);

                // First row is reserved for base palette
                for (int i = 0; i < ColorsPerPalette; i++) {
                    pixels.Data[i] = basePalette[i];
                }

                paletteTexture = new Texture(new Pixmap(pixels), TextureSizeMode.NonPowerOfTwo, TextureMagFilter.Nearest, TextureMinFilter.Nearest, TextureWrapMode.Repeat, TextureWrapMode.Clamp);

                RecreateGemPalettes();

                // First 3 rows are reserved (base palette + 2 gem palettes)
                lastPaletteRow = 2;
            } else {
                // Palette Texture already exists, update first (base) row
                ColorRgba[] pixels = paletteTexture.Res.BasePixmap.Res.PixelData[0].Data;

                for (int i = 0; i < ColorsPerPalette; i++) {
                    pixels[i] = basePalette[i];
                }

                //paletteTexture.Res.ReloadData();
                RecreateGemPalettes();
            }
        }

        public int AssignPalette(ColorRgba[] additionalPalette)
        {
#if DEBUG
            if (additionalPalette.Length > ColorsPerPalette) {
                throw new ArgumentOutOfRangeException(nameof(additionalPalette));
            }
#endif

            // Increment it first, because it points to last reserved palette
            lastPaletteRow++;

            ColorRgba[] palette = paletteTexture.Res.BasePixmap.Res.PixelData[0].Data;

            for (int i = 0; i < /*ColorsPerPalette*/additionalPalette.Length; i++) {
                palette[i + ColorsPerPalette * lastPaletteRow] = additionalPalette[i];
            }

            paletteTexture.Res.ReloadData();

            return lastPaletteRow;
        }

        private void RecreateGemPalettes()
        {
            const int GemColorCount = 4;
            const int Expansion = 32;

            int[] PaletteStops = {
                55, 52, 48, 15, 15,
                87, 84, 80, 15, 15,
                39, 36, 32, 15, 15,
                95, 92, 88, 15, 15
            };

            ColorRgba[] palette = paletteTexture.Res.BasePixmap.Res.PixelData[0].Data;

            // Start to fill palette texture from the second row (right after base palette)
            int src = 0, dst = ColorsPerPalette;
            for (int color = 0; color < GemColorCount; color++, src++) {
                // Compress 2 gem color gradients to single palette row
                for (int i = 0; i < (PaletteStops.Length / GemColorCount) - 1; i++) {
                    // Base Palette is in first row of "palette" array
                    ColorRgba from = palette[PaletteStops[src]];
                    ColorRgba to = palette[PaletteStops[++src]];

                    int r = from.R * 8, dr = (to.R * 8) - r;
                    int g = from.G * 8, dg = (to.G * 8) - g;
                    int b = from.B * 8, db = (to.B * 8) - b;
                    r *= Expansion; g *= Expansion; b *= Expansion;

                    for (int j = 0; j < Expansion; j++) {
                        palette[dst] = new ColorRgba(
                            (byte)(r / (8 * Expansion)),
                            (byte)(g / (8 * Expansion)),
                            (byte)(b / (8 * Expansion)),
                            255
                        );
                        r += dr; g += dg; b += db;
                        dst++;
                    }
                }
            }

            paletteTexture.Res.ReloadData();
        }
    }
}