using System;
using System.Drawing;
using System.Drawing.Imaging;
using Duality;
using Jazz2.Compatibility;

namespace Import
{
    public static class NormalMapGenerator
    {
        private struct Sampler
        {
            public Bitmap Source, Target;
            public Point FrameSize, FrameConfiguration;
            public Point CurrentFrame;
        }

        public static Bitmap FromSprite(Bitmap sprite, Point frameConfiguration, Color[] palette)
        {
            Point frameSize = new Point(sprite.Width / frameConfiguration.X, sprite.Height / frameConfiguration.Y);

            Bitmap normalMap = new Bitmap(sprite.Width, sprite.Height, PixelFormat.Format32bppArgb);
            Sampler sampler = new Sampler {
                Source = sprite,
                Target = normalMap,
                FrameSize = frameSize,
                FrameConfiguration = frameConfiguration
            };

            Vector2 step = new Vector2(-1f / frameSize.X, 1f / frameSize.Y);

            const float strength = 2.5f;
            const float level = 7.0f;
            float dz = (float)(1.0f / strength * (1.0 + Math.Pow(2.0f, level)));

            for (int i = 0; i < frameConfiguration.X; i++) {
                for (int j = 0; j < frameConfiguration.Y; j++) {
                    // Process every frame/tile separately
                    for (int x = 0; x < frameSize.X; x++) {
                        for (int y = 0; y < frameSize.Y; y++) {

                            sampler.CurrentFrame = new Point(i, j);

                            Vector2 uv = new Vector2((float)x / frameSize.X, (float)y / frameSize.Y);

                            Vector2 tlv = new Vector2(uv.X - step.X, uv.Y + step.Y);
                            Vector2 lv = new Vector2(uv.X - step.X, uv.Y);
                            Vector2 blv = new Vector2(uv.X - step.X, uv.Y - step.Y);
                            Vector2 tv = new Vector2(uv.X, uv.Y + step.Y);
                            Vector2 bv = new Vector2(uv.X, uv.Y - step.Y);
                            Vector2 trv = new Vector2(uv.X + step.X, uv.Y + step.Y);
                            Vector2 rv = new Vector2(uv.X + step.X, uv.Y);
                            Vector2 brv = new Vector2(uv.X + step.X, uv.Y - step.Y);

                            float tl = Texture2D(sampler, tlv, palette).X;
                            float l = Texture2D(sampler, lv, palette).X;
                            float bl = Texture2D(sampler, blv, palette).X;
                            float t = Texture2D(sampler, tv, palette).X;
                            float b = Texture2D(sampler, bv, palette).X;
                            float tr = Texture2D(sampler, trv, palette).X;
                            float r = Texture2D(sampler, rv, palette).X;
                            float br = Texture2D(sampler, brv, palette).X;

                            // Sobel
                            float dx = tl + l * 2.0f + bl - tr - r * 2.0f - br;
                            float dy = tl + t * 2.0f + tr - bl - b * 2.0f - br;
                            // Scharr
                            //float dx = tl * 3.0 + l * 10.0 + bl * 3.0 - tr * 3.0 - r * 10.0 - br * 3.0;
                            //float dy = tl * 3.0 + t * 10.0 + tr * 3.0 - bl * 3.0 - b * 10.0 - br * 3.0;

                            Vector4 normal = new Vector4(new Vector3(dx * 255.0f, dy * 255.0f, dz).Normalized, Texture2D(sampler, uv, palette).W);

                            RenderPixel(sampler, new Point(x, y), new Vector4(normal.X * 0.5f + 0.5f, normal.Y * 0.5f + 0.5f, normal.Z, normal.W));
                        }
                    }
                }
            }

            return normalMap;
        }

        private static Vector4 Texture2D(Sampler sampler, Vector2 coords, Color[] palette)
        {
            // Nearest neighbour sampling
            int x = MathF.Clamp((int)Math.Round(coords.X * sampler.FrameSize.X), 0, sampler.FrameSize.X - 1) + sampler.CurrentFrame.X * sampler.FrameSize.X;
            int y = MathF.Clamp((int)Math.Round(coords.Y * sampler.FrameSize.Y), 0, sampler.FrameSize.Y - 1) + sampler.CurrentFrame.Y * sampler.FrameSize.Y;

            Color color = sampler.Source.GetPixel(x, y);
            if (palette != null) {
                int alpha = color.A;
                color = palette[color.R];
                color = Color.FromArgb((int)(color.A * alpha / 255f), color);
            }

            // Convert to grayscale
            byte i = (byte)(((0x1d2f * color.B) + (0x9646 * color.G) + (0x4c8b * color.R)) >> 0x10);
            return new Vector4(i / 255f, i / 255f, i / 255f, color.A / 255f);
        }

        private static void RenderPixel(Sampler sampler, Point coords, Vector4 color)
        {
            int x = coords.X + sampler.CurrentFrame.X * sampler.FrameSize.X;
            int y = coords.Y + sampler.CurrentFrame.Y * sampler.FrameSize.Y;

            if (color.W == 0f) {
                // PNG optimization
                sampler.Target.SetPixel(x, y, Color.FromArgb(0, 0, 0, 0));
            } else {
                sampler.Target.SetPixel(x, y, Color.FromArgb((int)(color.W * 255), (int)(color.X * 255), (int)(color.Y * 255), (int)(color.Z * 255)));
            }
        }
    }
}