using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Duality;
using Duality.Drawing;
using Duality.IO;
using Duality.Resources;

namespace Jazz2.Game.UI
{
    public class BitmapFont
    {
        // ToDo: JJ2 uses different colors for menu and in-game
        private static readonly ColorRgba[] colors = {
            new ColorRgba(0.4f, 0.55f, 0.85f, 0.5f),
            new ColorRgba(0.7f, 0.45f, 0.42f, 0.5f),
            new ColorRgba(0.58f, 0.48f, 0.38f, 0.5f),
            new ColorRgba(0.25f, 0.45f, 0.3f, 0.5f),
            new ColorRgba(0.7f, 0.42f, 0.7f, 0.5f),
            new ColorRgba(0.44f, 0.44f, 0.8f, 0.5f),
            new ColorRgba(0.54f, 0.54f, 0.54f, 0.5f)
        };

        private ContentRef<Material> materialPlain, materialColor;
        private Rect[] asciiChars = new Rect[128];
        private Dictionary<int, Rect> unicodeChars = new Dictionary<int, Rect>();
        private int spacing, charHeight;

        private readonly Canvas canvas;

        public int Height => charHeight;

        // ToDo: Move parameters to .config file, rework .config file format
        public BitmapFont(Canvas canvas, string path)
        {
            this.canvas = canvas;

#if UNCOMPRESSED_CONTENT
            string png = PathOp.Combine(DualityApp.DataDirectory, "Animations", path + ".png");
#else
            string png = PathOp.Combine(DualityApp.DataDirectory, "Main.dz", "Animations", path + ".png");
#endif
            string pathFont = png + ".font";

            int textureHeight;

            using (Stream s = FileOp.Open(png, FileAccessMode.Read)) {
                PixelData pixelData = new Png(s).GetPixelData();
                textureHeight = pixelData.Height;

                ColorRgba[] palette = ContentResolver.Current.Palette.Res.BasePixmap.Res.MainLayer.Data;

                ColorRgba[] data = pixelData.Data;
                Parallel.ForEach(Partitioner.Create(0, data.Length), range => {
                    for (int i = range.Item1; i < range.Item2; i++) {
                        int colorIdx = data[i].R;
                        data[i] = palette[colorIdx].WithAlpha(palette[colorIdx].A * data[i].A / (255f * 255f));
                    }
                });

                Texture texture = new Texture(new Pixmap(pixelData), TextureSizeMode.NonPowerOfTwo, TextureMagFilter.Linear, TextureMinFilter.Linear);

                materialPlain = new Material(DrawTechnique.Alpha, texture);
                materialColor = new Material(ContentResolver.Current.RequestShader("Colorize"), texture);
            }

            using (Stream s = FileOp.Open(pathFont, FileAccessMode.Read)) {
                byte[] internalBuffer = new byte[128];

                byte flags = s.ReadUInt8(ref internalBuffer);
                ushort width = s.ReadUInt16(ref internalBuffer);
                ushort charHeight = s.ReadUInt16(ref internalBuffer);
                byte cols = s.ReadUInt8(ref internalBuffer);
                int rows = textureHeight / charHeight;
                short spacing = s.ReadInt16(ref internalBuffer);
                int asciiFirst = s.ReadUInt8(ref internalBuffer);
                int asciiCount = s.ReadUInt8(ref internalBuffer);

                s.Read(internalBuffer, 0, asciiCount);

                int i = 0;
                for (; i < asciiCount; i++) {
                    asciiChars[i + asciiFirst] = new Rect(
                        (float)(i % cols) / cols,
                        (float)(i / cols) / rows,
                        internalBuffer[i],
                        charHeight);
                }

                UTF8Encoding enc = new UTF8Encoding(false, true);

                int unicodeCharCount = asciiCount + s.ReadInt32(ref internalBuffer);
                for (; i < unicodeCharCount; i++) {
                    s.Read(internalBuffer, 0, 1);

                    int remainingBytes =
                        ((internalBuffer[0] & 240) == 240) ? 3 : (
                        ((internalBuffer[0] & 224) == 224) ? 2 : (
                        ((internalBuffer[0] & 192) == 192) ? 1 : -1
                    ));
                    if (remainingBytes == -1) {
                        throw new InvalidDataException("Char \"" + (char)internalBuffer[0] + "\" is not UTF-8");
                    }

                    s.Read(internalBuffer, 1, remainingBytes);
                    char c = enc.GetChars(internalBuffer, 0, remainingBytes + 1)[0];
                    byte charWidth = s.ReadUInt8(ref internalBuffer);

                    unicodeChars[c] = new Rect(
                        (float)(i % cols) / cols,
                        (float)(i / cols) / rows,
                        charWidth,
                        charHeight);
                }

                this.charHeight = charHeight;
                this.spacing = spacing;
            }
        }

        public unsafe void DrawString(ref int charOffset, string text, float x, float y, Alignment alignment, ColorRgba? color = null, float scale = 1f, float angleOffset = 0f, float varianceX = 4f, float varianceY = 4f, float speed = 4f, float charSpacing = 1f, float lineSpacing = 1f)
        {
            if (string.IsNullOrEmpty(text)) {
                return;
            }

            float phase = (float)Time.GameTimer.TotalSeconds * speed;

            bool hasColor = false;
            // Pre-compute text size
            //int lines = 1;
            float totalWidth = 0f, lastWidth = 0f, totalHeight = 0f;
            float charSpacingPre = charSpacing;
            float scalePre = scale;
            for (int i = 0; i < text.Length; i++) {
                if (text[i] == '\n') {
                    if (lastWidth < totalWidth) {
                        lastWidth = totalWidth;
                    }
                    totalWidth = 0f;
                    totalHeight += (charHeight * scale * lineSpacing);
                    //lines++;
                    continue;
                } else if (text[i] == '\f' && text[i + 1] == '[') {
                    i += 2;
                    int formatIndex = i;
                    while (text[i] != ']') {
                        i++;
                    }

                    if (text[formatIndex + 1] == ':') {
                        int paramInt;
                        switch (text[formatIndex]) {
                            case 'c': // Color
                                hasColor = true;
                                break;
                            case 's': // Scale
                                      //if (int.TryParse(new string(ptr, formatIndex + 2, i - (formatIndex + 2)), out paramInt)) {
                                if (int.TryParse(text.Substring(formatIndex + 2, i - (formatIndex + 2)), out paramInt)) {
                                    scalePre = paramInt * 0.01f;
                                }
                                break;
                            case 'w': // Char spacing
                                      //if (int.TryParse(new string(ptr, formatIndex + 2, i - (formatIndex + 2)), out paramInt)) {
                                if (int.TryParse(text.Substring(formatIndex + 2, i - (formatIndex + 2)), out paramInt)) {
                                    charSpacingPre = paramInt * 0.01f;
                                }
                                break;
                        }
                    }
                    continue;
                }

                Rect uvRect;
                if (!unicodeChars.TryGetValue(text[i], out uvRect)) {
                    byte ascii = (byte)text[i];
                    if (ascii < 128) {
                        uvRect = asciiChars[ascii];
                    } else {
                        uvRect = new Rect();
                    }
                }

                if (uvRect.W > 0 && uvRect.H > 0) {
                    totalWidth += (uvRect.W + spacing) * charSpacingPre * scalePre;
                }
            }
            if (lastWidth < totalWidth) {
                lastWidth = totalWidth;
            }
            totalHeight += (charHeight * scale * lineSpacing);

            VertexC1P3T2[] vertexData = canvas.RentVertices(text.Length * 4);

            // Set default material
            bool colorize, allowColorChange;
            ContentRef<Material> material;
            ColorRgba mainColor;
            if (color.HasValue) {
                mainColor = color.Value;
                if (mainColor == ColorRgba.TransparentBlack) {
                    if (hasColor) {
                        material = materialColor;
                        mainColor = new ColorRgba(0.46f, 0.46f, 0.4f, 0.5f);
                    } else {
                        material = materialPlain;
                        mainColor = ColorRgba.White;
                    }
                } else {
                    material = materialColor;
                }
                colorize = false;

                if (mainColor.R == 0 && mainColor.G == 0 && mainColor.B == 0) {
                    allowColorChange = false;
                } else {
                    allowColorChange = true;
                }
            } else {
                material = materialColor;
                mainColor = ColorRgba.White;
                colorize = true;
                allowColorChange = false;
            }

            Vector2 uvRatio = new Vector2(
                1f / materialPlain.Res.MainTexture.Res.ContentWidth,
                1f / materialPlain.Res.MainTexture.Res.ContentHeight
            );

            int vertexIndex = 0;

            Vector2 originPos = new Vector2(x, y);
            alignment.ApplyTo(ref originPos, new Vector2(lastWidth /** scale*/, totalHeight/*lines * height * scale * lineSpacing*/));
            float lineStart = originPos.X;

            for (int i = 0; i < text.Length; i++) {
                if (text[i] == '\n') {
                    // New line
                    originPos.X = lineStart;
                    originPos.Y += (charHeight * scale * lineSpacing);
                    continue;
                } else if (text[i] == '\f' && text[i + 1] == '[') {
                    // Format
                    i += 2;
                    int formatIndex = i;
                    while (text[i] != ']') {
                        i++;
                    }

                    if (text[formatIndex + 1] == ':') {
                        int paramInt;
                        switch (text[formatIndex]) {
                            case 'c': // Color
                                //if (allowColorChange && int.TryParse(new string(ptr, formatIndex + 2, i - (formatIndex + 2)), out paramInt)) {
                                if (allowColorChange && int.TryParse(text.Substring(formatIndex + 2, i - (formatIndex + 2)), out paramInt)) {
                                    if (paramInt == -1) {
                                        colorize = true;
                                    } else {
                                        colorize = false;
                                        mainColor = colors[paramInt % colors.Length];
                                    }
                                }
                                break;
                            case 's': // Scale
                                //if (int.TryParse(new string(ptr, formatIndex + 2, i - (formatIndex + 2)), out paramInt)) {
                                if (int.TryParse(text.Substring(formatIndex + 2, i - (formatIndex + 2)), out paramInt)) {
                                    scale = paramInt * 0.01f;
                                }
                                break;
                            case 'w': // Char spacing
                                //if (int.TryParse(new string(ptr, formatIndex + 2, i - (formatIndex + 2)), out paramInt)) {
                                if (int.TryParse(text.Substring(formatIndex + 2, i - (formatIndex + 2)), out paramInt)) {
                                    charSpacing = paramInt * 0.01f;
                                }
                                break;

                            default:
                                // Unknown formatting
                                break;
                        }
                    }
                    continue;
                }

                Rect uvRect;
                if (!unicodeChars.TryGetValue(text[i], out uvRect)) {
                    byte ascii = (byte)text[i];
                    if (ascii < 128) {
                        uvRect = asciiChars[ascii];
                    } else {
                        uvRect = new Rect();
                    }
                }

                if (uvRect.W > 0 && uvRect.H > 0) {
                    if (colorize) {
                        mainColor = colors[charOffset % colors.Length];
                    }

                    Vector3 pos = new Vector3(originPos);

                    if (angleOffset > 0f) {
                        pos.X += MathF.Cos((phase + charOffset) * angleOffset * MathF.Pi) * varianceX * scale;
                        pos.Y += MathF.Sin((phase + charOffset) * angleOffset * MathF.Pi) * varianceY * scale;
                    }

                    pos.X = MathF.Round(pos.X);
                    pos.Y = MathF.Round(pos.Y);

                    float x2 = MathF.Round(pos.X + uvRect.W * scale);
                    float y2 = MathF.Round(pos.Y + uvRect.H * scale);

                    vertexData[vertexIndex + 0].Pos = pos;
                    vertexData[vertexIndex + 0].TexCoord.X = uvRect.X;
                    vertexData[vertexIndex + 0].TexCoord.Y = uvRect.Y;
                    vertexData[vertexIndex + 0].Color = mainColor;

                    vertexData[vertexIndex + 1].Pos.X = pos.X;
                    vertexData[vertexIndex + 1].Pos.Y = y2;
                    vertexData[vertexIndex + 1].Pos.Z = pos.Z;
                    vertexData[vertexIndex + 1].TexCoord.X = uvRect.X;
                    vertexData[vertexIndex + 1].TexCoord.Y = uvRect.Y + uvRect.H * uvRatio.Y;
                    vertexData[vertexIndex + 1].Color = mainColor;

                    vertexData[vertexIndex + 2].Pos.X = x2;
                    vertexData[vertexIndex + 2].Pos.Y = y2;
                    vertexData[vertexIndex + 2].Pos.Z = pos.Z;
                    vertexData[vertexIndex + 2].TexCoord.X = uvRect.X + uvRect.W * uvRatio.X;
                    vertexData[vertexIndex + 2].TexCoord.Y = uvRect.Y + uvRect.H * uvRatio.Y;
                    vertexData[vertexIndex + 2].Color = mainColor;

                    vertexData[vertexIndex + 3].Pos.X = x2;
                    vertexData[vertexIndex + 3].Pos.Y = pos.Y;
                    vertexData[vertexIndex + 3].Pos.Z = pos.Z;
                    vertexData[vertexIndex + 3].TexCoord.X = uvRect.X + uvRect.W * uvRatio.X;
                    vertexData[vertexIndex + 3].TexCoord.Y = uvRect.Y;
                    vertexData[vertexIndex + 3].Color = mainColor;

                    if (MathF.RoundToInt(canvas.DrawDevice.TargetSize.X) != (MathF.RoundToInt(canvas.DrawDevice.TargetSize.X) / 2) * 2) {
                        float align = 0.5f / canvas.DrawDevice.TargetSize.X;

                        vertexData[vertexIndex + 0].Pos.X += align;
                        vertexData[vertexIndex + 1].Pos.X += align;
                        vertexData[vertexIndex + 2].Pos.X += align;
                        vertexData[vertexIndex + 3].Pos.X += align;
                    }

                    if (MathF.RoundToInt(canvas.DrawDevice.TargetSize.Y) != (MathF.RoundToInt(canvas.DrawDevice.TargetSize.Y) / 2) * 2) {
                        float align = 0.5f * scale / canvas.DrawDevice.TargetSize.Y;

                        vertexData[vertexIndex + 0].Pos.Y += align;
                        vertexData[vertexIndex + 1].Pos.Y += align;
                        vertexData[vertexIndex + 2].Pos.Y += align;
                        vertexData[vertexIndex + 3].Pos.Y += align;
                    }

                    vertexIndex += 4;

                    originPos.X += ((uvRect.W + spacing) * scale * charSpacing);
                }
                charOffset++;
            }
            charOffset++;

            // Submit all the vertices as one draw batch
            canvas.DrawDevice.AddVertices(
                material,
                VertexMode.Quads,
                vertexData,
                0,
                vertexIndex);
        }

        public static string StripFormatting(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.IndexOf('\f') == -1) {
                return text;
            }

            StringBuilder sb = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++) {
                if (text[i] == '\f' && i + 2 < text.Length && text[i + 1] == '[') {
                    i = text.IndexOf(']', i);
                } else {
                    sb.Append(text[i]);
                }
            }
            return sb.ToString();
        }
    }
}