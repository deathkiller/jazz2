using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using Duality;

namespace Jazz2.Compatibility
{
    public class JJ2Episode // .j2e
    {
        private int position;
        private string episodeToken, episodeName, firstLevel;
        private bool isRegistered;

        //private Bitmap image, titleLight, titleDark;
        private Bitmap titleLight;

        public int Position => position;
        public string Token => episodeToken;
        public string Name => episodeName;
        public string FirstLevel => firstLevel;

        public static JJ2Episode Open(string path)
        {
            using (Stream s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader r = new BinaryReader(s)) {
                JJ2Episode episode = new JJ2Episode();

                episode.episodeToken = Path.GetFileNameWithoutExtension(path).ToLower(CultureInfo.InvariantCulture);

                // ToDo: Implement JJ2+ extended data
                // the condition of unlocking (currently only defined for 0 meaning "always unlocked"
                // and 1 meaning "requires the previous episode to be finished", stored as a 4-byte-long
                // integer starting at byte 0x4), binary flags of various purpose (currently supported
                // flags are 1 and 2 used to reset respectively player ammo and lives when the episode
                // begins; stored as a 4-byte-long integer starting at byte 0x8), file name of the preceding
                // episode (used mostly to determine whether the episode should be locked, stored
                // as a 32-byte-long chain of characters starting at byte 0x4C), file name of the following
                // episode (that is cycled to after the episode ends, stored as a 32-byte-long
                // chain of characters starting at byte 0x6C)

                // Header (208 bytes)
                int headerSize = r.ReadInt32();
                episode.position = r.ReadInt32();
                episode.isRegistered = (r.ReadInt32() != 0);
                int unknown1 = r.ReadInt32();

                {
                    byte[] episodeNameRaw = r.ReadBytes(128);
                    episode.episodeName = Encoding.ASCII.GetString(episodeNameRaw);
                    int i = episode.episodeName.IndexOf('\0');
                    if (i != -1) {
                        episode.episodeName = episode.episodeName.Substring(0, i);
                    }
                }
                {
                    byte[] firstLevelRaw = r.ReadBytes(32);
                    episode.firstLevel = Encoding.ASCII.GetString(firstLevelRaw);
                    int i = episode.firstLevel.IndexOf('\0');
                    if (i != -1) {
                        episode.firstLevel = episode.firstLevel.Substring(0, i);
                    }
                }

                // ToDo: Episode images are not supported yet
                int width = r.ReadInt32();
                int height = r.ReadInt32();
                int unknown2 = r.ReadInt32();
                int unknown3 = r.ReadInt32();

                int titleWidth = r.ReadInt32();
                int titleHeight = r.ReadInt32();
                int unknown4 = r.ReadInt32();
                int unknown5 = r.ReadInt32();

                {
                    int imagePackedSize = r.ReadInt32();
                    int imageUnpackedSize = width * height;
                    JJ2Block imageBlock = new JJ2Block(s, imagePackedSize, imageUnpackedSize);
                    //episode.image = ConvertIndicesToRgbaBitmap(width, height, imageBlock, false);
                }
                {
                    int titleLightPackedSize = r.ReadInt32();
                    int titleLightUnpackedSize = titleWidth * titleHeight;
                    JJ2Block titleLightBlock = new JJ2Block(s, titleLightPackedSize, titleLightUnpackedSize);
                    episode.titleLight = ConvertIndicesToRgbaBitmap(titleWidth, titleHeight, titleLightBlock, true);
                }
                //{
                //    int titleDarkPackedSize = r.ReadInt32();
                //    int titleDarkUnpackedSize = titleWidth * titleHeight;
                //    JJ2Block titleDarkBlock = new JJ2Block(s, titleDarkPackedSize, titleDarkUnpackedSize);
                //    episode.titleDark = ConvertIndicesToRgbaBitmap(titleWidth, titleHeight, titleDarkBlock, true);
                //}

                return episode;
            }
        }

        public void Convert(string path, Func<string, JJ2Level.LevelToken> levelTokenConversion = null, Func<JJ2Episode, string> episodeNameConversion = null, Func<JJ2Episode, Tuple<string, string>> episodePrevNext = null)
        {
            using (Stream s = File.Create(Path.Combine(path, ".res")))
            using (StreamWriter w = new StreamWriter(s, new UTF8Encoding(false))) {
                w.WriteLine("{");
                w.WriteLine("    \"Version\": {");
                w.WriteLine("        \"Target\": \"Jazz² Resurrection\"");
                w.WriteLine("    },");

                string name = episodeName;
                if (episodeNameConversion != null) {
                    name = episodeNameConversion(this);
                }

                w.WriteLine("    \"Name\": \"" + name + "\",");
                w.WriteLine("    \"Position\": " + position + ",");

                if (firstLevel.EndsWith(".j2l", StringComparison.InvariantCultureIgnoreCase) ||
                    firstLevel.EndsWith(".lev", StringComparison.InvariantCultureIgnoreCase)) {
                    firstLevel = firstLevel.Substring(0, firstLevel.Length - 4);
                }

                if (levelTokenConversion != null) {
                    JJ2Level.LevelToken token = levelTokenConversion(firstLevel);
                    firstLevel = token.Level;
                }

                w.Write("    \"FirstLevel\": \"" + firstLevel + "\"");

                if (episodePrevNext != null) {
                    var prevNext = episodePrevNext(this);
                    if (!string.IsNullOrEmpty(prevNext.Item1)) {
                        w.WriteLine(",");
                        w.Write("    \"PreviousEpisode\": \"" + prevNext.Item1 + "\"");
                    }

                    if (!string.IsNullOrEmpty(prevNext.Item2)) {
                        w.WriteLine(",");
                        w.Write("    \"NextEpisode\": \"" + prevNext.Item2 + "\"");
                    }
                }

                w.WriteLine();
                w.Write("}");
            }

            // ToDo: Episode images are not supported yet
            //if (image != null) {
            //    image.Save(Path.Combine(path, "image.png"), ImageFormat.Png);
            //}

            //if (titleLight != null) {
            //    titleLight.Save(Path.Combine(path, "Title.png"), ImageFormat.Png);
            //}

            //if (titleDark != null) {
            //    titleDark.Save(Path.Combine(path, "titleDark.png"), ImageFormat.Png);
            //}

            if (titleLight != null) {
                // Resize the original image
                const float ratio = 120f / 220f;
                int width = MathF.RoundToInt(titleLight.Width * ratio);
                int height = MathF.RoundToInt(titleLight.Height * ratio);
                Bitmap title = new Bitmap(width, height);
                using (Graphics g = Graphics.FromImage(title)) {
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.InterpolationMode = InterpolationMode.High;
                    g.SmoothingMode = SmoothingMode.HighQuality;

                    g.DrawImage(titleLight, new Rectangle(0, 0, width, height));
                }

                // Align image to center
                int left = 0, right = 0;
                for (int x = 0; x < width; x++) {
                    for (int y = 0; y < height; y++) {
                        Color color = title.GetPixel(x, y);
                        if (color.A > 0) {
                            left = x;
                            break;
                        }
                    }
                }

                for (int x = width - 1; x >= 0; x--) {
                    for (int y = 0; y < height; y++) {
                        Color color = title.GetPixel(x, y);
                        if (color.A > 0) {
                            right = x;
                            break;
                        }
                    }
                }

                int align = ((width - right - 1) - left) / 2;

                // Shadow
                Bitmap shadow = new Bitmap(width, height);

                for (int x = 0; x < width; x++) {
                    for (int y = 0; y < height; y++) {
                        Color color = title.GetPixel(x, y);
                        if (color.A > 0) {
                            color = Color.FromArgb(color.A, 0, 20, 30);
                        }
                        shadow.SetPixel(x, y, color);
                    }
                }

                // Compose final image
                Bitmap output = new Bitmap(width, height);

                using (Graphics g = Graphics.FromImage(output)) {
                    DrawImageEx(g, shadow, new RectangleF(align, -0.4f, width, height), 100, false);
                    DrawImageEx(g, shadow, new RectangleF(align, 1.2f, width, height), 200, false);

                    g.DrawImage(title, new Rectangle(align, 0, width, height));
                }

                output.Save(Path.Combine(path, ".png"), ImageFormat.Png);
            }
        }

        private static Bitmap ConvertIndicesToRgbaBitmap(int width, int height, JJ2Block block, bool removeShadow)
        {
            byte[] data = block.AsByteArray();

            Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    int index = data[y * width + x];
                    // Use menu palette here
                    Color color;
                    if (removeShadow && (index == 63 || index == 143)) {
                        color = Color.FromArgb(0, 0, 0, 0);
                    } else {
                        color = JJ2DefaultPalette.Menu[index];
                    }

                    result.SetPixel(x, y, color);
                }
            }

            return result;
        }

        public static void DrawImageEx(Graphics g, Image i, RectangleF r, byte alpha, bool grayscaled)
        {
            if (alpha == 0)
                return;

            PixelOffsetMode oldPOM = g.PixelOffsetMode;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            PointF ulCorner = new PointF(r.Left, r.Top);
            PointF urCorner = new PointF(r.Right, r.Top);
            PointF llCorner = new PointF(r.Left, r.Bottom);
            PointF[] destPoints = { ulCorner, urCorner, llCorner };

            if (alpha == 0xff && !grayscaled) {
                g.DrawImage(i, destPoints, new RectangleF(0, 0, i.Width, i.Height), GraphicsUnit.Pixel);
                return;
            }

            ColorMatrix colorMatrix;
            if (grayscaled) {
                colorMatrix = new ColorMatrix(new[] {
                    new float[] {0.299f, 0.299f, 0.299f, 0, 0},
                    new float[] {0.587f, 0.587f, 0.587f, 0, 0},
                    new float[] {0.114f, 0.114f, 0.114f, 0, 0},
                    new float[] {0, 0, 0, alpha / 255f, 0},
                    new float[] {0, 0, 0, 0, 1}
                });
            } else {
                colorMatrix = new ColorMatrix(new[] {
                    new float[] {1, 0, 0, 0, 0},
                    new float[] {0, 1, 0, 0, 0},
                    new float[] {0, 0, 1, 0, 0},
                    new float[] {0, 0, 0, alpha / 255f, 0},
                    new float[] {0, 0, 0, 0, 1}
                });
            }
            using (ImageAttributes imageAttributes = new ImageAttributes()) {
                imageAttributes.SetColorMatrix(colorMatrix);
                g.DrawImage(i, destPoints, new RectangleF(0, 0, i.Width, i.Height), GraphicsUnit.Pixel, imageAttributes);
            }
            g.PixelOffsetMode = oldPOM;
        }
    }
}