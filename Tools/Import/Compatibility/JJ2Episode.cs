using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using Duality;
using Duality.Drawing;
using Import;
using MathF = Duality.MathF;

namespace Jazz2.Compatibility
{
    public class JJ2Episode // .j2e
    {
        private int position;
        private string episodeToken, episodeName, firstLevel;
        private bool isRegistered;

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

                // ToDo: Implement JJ2+ extended data, but I haven't seen it anywhere yet
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

                // Episode name
                {
                    byte[] episodeNameRaw = r.ReadBytes(128);
                    episode.episodeName = Encoding.ASCII.GetString(episodeNameRaw);
                    int i = episode.episodeName.IndexOf('\0');
                    if (i != -1) {
                        episode.episodeName = episode.episodeName.Substring(0, i);
                    }
                }
                // First level
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

        private JJ2Episode()
        {
        }

        public JJ2Episode(string episodeToken, string episodeName, string firstLevel, int position)
        {
            this.episodeToken = episodeToken;
            this.episodeName = episodeName;
            this.firstLevel = firstLevel;
            this.position = position;
        }

        public void Convert(string path, Func<string, JJ2Level.LevelToken> levelTokenConversion = null, Func<JJ2Episode, string> episodeNameConversion = null, Func<JJ2Episode, Tuple<string, string>> episodePrevNext = null)
        {
            using (Stream s = File.Create(Path.Combine(path, "Episode.res")))
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

            if (titleLight != null) {
                PngWriter output = ConvertEpisodeLogo(titleLight);
                output.Save(Path.Combine(path, "Logo.png"));
            }
        }

        private static PngWriter ConvertEpisodeLogo(Bitmap logo)
        {
            // Resize the original image
            const float LogoResizeRatio = 120f / 220f;

            int width = MathF.RoundToInt(logo.Width * LogoResizeRatio);
            int height = MathF.RoundToInt(logo.Height * LogoResizeRatio);
            Bitmap title = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(title)) {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.High;
                g.SmoothingMode = SmoothingMode.HighQuality;

                g.DrawImage(logo, new Rectangle(0, 0, width, height));
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

            int alignX = ((width - right - 1) - left) / 2;

            // Shadow
            Bitmap shadow = new Bitmap(width, height);

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    Color color = title.GetPixel(x, y);
                    if (color.A > 0) {
                        color = Color.FromArgb(color.A, 0, 20, 30);
                    }
                    shadow.SetPixel(x, y, color);
                }
            }

            // Compose final image
            Bitmap result = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(result)) {
                g.DrawImageEx(shadow, new RectangleF(alignX, -0.4f, width, height), 100, false);
                g.DrawImageEx(shadow, new RectangleF(alignX, 1.2f, width, height), 200, false);

                g.DrawImage(title, new Rectangle(alignX, 0, width, height));
            }

            return new PngWriter(result);
        }

        private static Bitmap ConvertIndicesToRgbaBitmap(int width, int height, JJ2Block block, bool removeShadow)
        {
            byte[] data = block.AsByteArray();

            Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    int index = data[y * width + x];
                    // Use menu palette here
                    ColorRgba color;
                    if (removeShadow && (index == 63 || index == 143)) {
                        // Remove original shadow pixels
                        color = new ColorRgba(0);
                    } else {
                        color = JJ2DefaultPalette.Menu[index];
                    }

                    result.SetPixel(x, y, Color.FromArgb(color.A, color.R, color.G, color.B));
                }
            }

            return result;
        }
    }
}