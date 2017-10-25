using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using Duality.Drawing;
using Editor;
using SysDrawFont = System.Drawing.Font;
using SysDrawFontStyle = System.Drawing.FontStyle;

namespace Duality.Resources
{
    public class FontRasterizer
    {
        private FontData fontData;
        private float spacing = 0.0f;
	    private float lineHeightFactor = 1.0f;
	    private bool kerning = true;
	    // Data that is automatically acquired while loading the font
	    private int[] charLookup = null;
	    private Material material = null;
	    private Texture texture = null;
	    private Pixmap pixmap = null;
	    private FontKerningLookup kerningLookup = null;

        /// <summary>
        /// [GET] The <see cref="Duality.Resources.Material"/> to use when rendering text of this Font.
        /// </summary>
        public Material Material
        {
            get { return this.material; }
        }
        /// <summary>
        /// [GET / SET] Additional spacing between each character.
        /// </summary>
        public float CharSpacing
        {
            get { return this.spacing; }
            set { this.spacing = value; }
        }
        /// <summary>
        /// [GET / SET] A factor for the Fonts <see cref="Height"/> value that affects line spacings but not actual glyph sizes.
        /// </summary>
        public float LineHeightFactor
        {
            get { return this.lineHeightFactor; }
            set { this.lineHeightFactor = value; }
        }
        /// <summary>
        /// [GET / SET] Whether this Font uses kerning, a technique where characters are moved closer together based on their actual shape,
        /// which usually looks much nicer. It has no visual effect when active at the same time with <see cref="FontMetrics.Monospace"/>, however
        /// kerning sample data will be available on glyphs.
        /// </summary>
        /// <seealso cref="FontGlyphData"/>
        public bool Kerning
        {
            get { return this.kerning; }
            set { this.kerning = value; }
        }
        /// <summary>
        /// [GET] The Fonts height.
        /// </summary>
        public int Height
        {
            get { return this.fontData.Metrics.Height; }
        }
        /// <summary>
        /// [GET] The y offset in pixels between two lines.
        /// </summary>
        public int LineSpacing
        {
            get { return MathF.RoundToInt(this.fontData.Metrics.Height * this.lineHeightFactor); }
        }
        /// <summary>
        /// [GET] Provides access to various metrics that are inherent to this <see cref="Font"/> instance,
        /// such as size, height, and various typographic measures.
        /// </summary>
        public FontMetrics Metrics
        {
            get { return this.fontData.Metrics; }
        }

        public FontRasterizer(string fontFamily, float emSize, FontStyle style, string extendedSet, bool antialiasing, bool monospace, FontRenderMode renderMode)
		{
            fontData = this.RenderGlyphs(
                fontFamily,
                emSize,
                style,
                !string.IsNullOrEmpty(extendedSet) ? new FontCharSet(extendedSet) : null,
                antialiasing,
                monospace);

	        FontGlyphData[] glyphs = fontData.Glyphs;
	        if (glyphs == null) {
		        this.charLookup = new int[0];
		        return;
	        }

	        int maxCharVal = 0;
	        for (int i = 0; i < glyphs.Length; i++) {
		        maxCharVal = Math.Max(maxCharVal, (int)glyphs[i].Glyph);
	        }

	        this.charLookup = new int[maxCharVal + 1];
	        for (int i = 0; i < glyphs.Length; i++) {
		        this.charLookup[(int)glyphs[i].Glyph] = i;
	        }


	        this.pixmap = new Pixmap(fontData.Bitmap);
	        this.pixmap.Atlas = fontData.Atlas.ToList();

		    bool isPixelGridAligned =
		        renderMode == FontRenderMode.MonochromeBitmap || renderMode == FontRenderMode.GrayscaleBitmap;

            this.texture = new Texture(this.pixmap,
		        TextureSizeMode.Enlarge,
                isPixelGridAligned ? TextureMagFilter.Nearest : TextureMagFilter.Linear,
                isPixelGridAligned ? TextureMinFilter.Nearest : TextureMinFilter.LinearMipmapLinear);

	        // Select DrawTechnique to use
	        ContentRef<DrawTechnique> technique;
	        if (renderMode == FontRenderMode.MonochromeBitmap)
		        technique = DrawTechnique.Mask;
	        else if (renderMode == FontRenderMode.GrayscaleBitmap)
		        technique = DrawTechnique.Alpha;
	        else if (renderMode == FontRenderMode.SmoothBitmap)
		        technique = DrawTechnique.Alpha;
	        else
		        technique = DrawTechnique.SharpAlpha;

	        // Create and configure internal BatchInfo
	        BatchInfo matInfo = new BatchInfo(technique, this.texture);
	        if (technique == DrawTechnique.SharpAlpha) {
		        matInfo.SetValue("smoothness", this.fontData.Metrics.Size * 4.0f);
	        }
	        this.material = new Material(matInfo);

	        this.kerningLookup = new FontKerningLookup(this.fontData.KerningPairs);
		}


	    /// <summary>
	    /// Retrieves information about a single glyph.
	    /// </summary>
	    /// <param name="glyph">The glyph to retrieve information about.</param>
	    /// <param name="data">A struct holding the retrieved information.</param>
	    /// <returns>True, if successful, false if the specified glyph is not supported.</returns>
	    public bool GetGlyphData(char glyph, out FontGlyphData data)
	    {
		    int glyphId = (int)glyph;
		    if (glyphId >= this.charLookup.Length) {
			    data = this.fontData.Glyphs[0];
			    return false;
		    } else {
			    data = this.fontData.Glyphs[this.charLookup[glyphId]];
			    return true;
		    }
	    }

		/// <summary>
		/// Emits a set of vertices based on a text. To render this text, simply use that set of vertices combined with
		/// the Fonts <see cref="Material"/>.
		/// </summary>
		/// <param name="text">The text to render.</param>
		/// <param name="vertices">The set of vertices that is emitted. You can re-use the same array each frame.</param>
		/// <param name="x">An X-Offset applied to the position of each emitted vertex.</param>
		/// <param name="y">An Y-Offset applied to the position of each emitted vertex.</param>
		/// <param name="z">An Z-Offset applied to the position of each emitted vertex.</param>
		/// <returns>The number of emitted vertices. This values isn't necessarily equal to the emitted arrays length.</returns>
		public int EmitTextVertices(string text, ref VertexC1P3T2[] vertices, float x, float y, float z = 0.0f)
		{
			return this.EmitTextVertices(text, ref vertices, x, y, z, ColorRgba.White);
		}
		/// <summary>
		/// Emits a set of vertices based on a text. To render this text, simply use that set of vertices combined with
		/// the Fonts <see cref="Material"/>.
		/// </summary>
		/// <param name="text">The text to render.</param>
		/// <param name="vertices">The set of vertices that is emitted. You can re-use the same array each frame.</param>
		/// <param name="x">An X-Offset applied to the position of each emitted vertex.</param>
		/// <param name="y">An Y-Offset applied to the position of each emitted vertex.</param>
		/// <param name="z">An Z-Offset applied to the position of each emitted vertex.</param>
		/// <param name="clr">The color value that is applied to each emitted vertex.</param>
		/// <param name="angle">An angle by which the text is rotated (before applying the offset).</param>
		/// <param name="scale">A factor by which the text is scaled (before applying the offset).</param>
		/// <returns>The number of emitted vertices. This values isn't necessarily equal to the emitted arrays length.</returns>
		public int EmitTextVertices(string text, ref VertexC1P3T2[] vertices, float x, float y, float z, ColorRgba clr, float angle = 0.0f, float scale = 1.0f)
		{
			int len = this.EmitTextVertices(text, ref vertices);

			Vector3 offset = new Vector3(x, y, z);
			Vector2 xDot, yDot;
			MathF.GetTransformDotVec(angle, scale, out xDot, out yDot);

			for (int i = 0; i < len; i++) {
				MathF.TransformDotVec(ref vertices[i].Pos, ref xDot, ref yDot);
				Vector3.Add(ref vertices[i].Pos, ref offset, out vertices[i].Pos);
				vertices[i].Color = clr;
			}

			return len;
		}
		/// <summary>
		/// Emits a set of vertices based on a text. To render this text, simply use that set of vertices combined with
		/// the Fonts <see cref="Material"/>.
		/// </summary>
		/// <param name="text">The text to render.</param>
		/// <param name="vertices">The set of vertices that is emitted. You can re-use the same array each frame.</param>
		/// <param name="x">An X-Offset applied to the position of each emitted vertex.</param>
		/// <param name="y">An Y-Offset applied to the position of each emitted vertex.</param>
		/// <param name="clr">The color value that is applied to each emitted vertex.</param>
		/// <returns>The number of emitted vertices. This values isn't necessarily equal to the emitted arrays length.</returns>
		public int EmitTextVertices(string text, ref VertexC1P3T2[] vertices, float x, float y, ColorRgba clr)
		{
			int len = this.EmitTextVertices(text, ref vertices);

			Vector3 offset = new Vector3(x, y, 0);

			for (int i = 0; i < len; i++) {
				Vector3.Add(ref vertices[i].Pos, ref offset, out vertices[i].Pos);
				vertices[i].Color = clr;
			}

			return len;
		}
		/// <summary>
		/// Emits a set of vertices based on a text. To render this text, simply use that set of vertices combined with
		/// the Fonts <see cref="Material"/>.
		/// </summary>
		/// <param name="text">The text to render.</param>
		/// <param name="vertices">The set of vertices that is emitted. You can re-use the same array each frame.</param>
		/// <returns>The number of emitted vertices. This values isn't necessarily equal to the emitted arrays length.</returns>
		public int EmitTextVertices(string text, ref VertexC1P3T2[] vertices)
		{
			int len = text.Length * 4;
			if (vertices == null || vertices.Length < len) vertices = new VertexC1P3T2[len];

			if (this.texture == null)
				return len;

			float curOffset = 0.0f;
			FontGlyphData glyphData;
			Rect uvRect;
			float glyphXAdv;
			for (int i = 0; i < text.Length; i++) {
				this.ProcessTextAdv(text, i, out glyphData, out uvRect, out glyphXAdv);

				Vector2 glyphPos;
				glyphPos.X = MathF.Round(curOffset - glyphData.Offset.X);
				glyphPos.Y = MathF.Round(0 - glyphData.Offset.Y);

				vertices[i * 4 + 0].Pos.X = glyphPos.X;
				vertices[i * 4 + 0].Pos.Y = glyphPos.Y;
				vertices[i * 4 + 0].Pos.Z = 0.0f;
				vertices[i * 4 + 0].TexCoord = uvRect.TopLeft;
				vertices[i * 4 + 0].Color = ColorRgba.White;

				vertices[i * 4 + 1].Pos.X = glyphPos.X + glyphData.Size.X;
				vertices[i * 4 + 1].Pos.Y = glyphPos.Y;
				vertices[i * 4 + 1].Pos.Z = 0.0f;
				vertices[i * 4 + 1].TexCoord = uvRect.TopRight;
				vertices[i * 4 + 1].Color = ColorRgba.White;

				vertices[i * 4 + 2].Pos.X = glyphPos.X + glyphData.Size.X;
				vertices[i * 4 + 2].Pos.Y = glyphPos.Y + glyphData.Size.Y;
				vertices[i * 4 + 2].Pos.Z = 0.0f;
				vertices[i * 4 + 2].TexCoord = uvRect.BottomRight;
				vertices[i * 4 + 2].Color = ColorRgba.White;

				vertices[i * 4 + 3].Pos.X = glyphPos.X;
				vertices[i * 4 + 3].Pos.Y = glyphPos.Y + glyphData.Size.Y;
				vertices[i * 4 + 3].Pos.Z = 0.0f;
				vertices[i * 4 + 3].TexCoord = uvRect.BottomLeft;
				vertices[i * 4 + 3].Color = ColorRgba.White;

				curOffset += glyphXAdv;
			}

			return len;
		}

		/// <summary>
		/// Measures the size of a text rendered using this Font.
		/// </summary>
		/// <param name="text">The text to measure.</param>
		/// <returns>The size of the measured text.</returns>
		public Vector2 MeasureText(string text)
		{
			if (this.texture == null || text == null) return Vector2.Zero;

			Vector2 textSize = Vector2.Zero;
			float curOffset = 0.0f;
			FontGlyphData glyphData;
			Rect uvRect;
			float glyphXAdv;
			for (int i = 0; i < text.Length; i++) {
				this.ProcessTextAdv(text, i, out glyphData, out uvRect, out glyphXAdv);

				textSize.X = Math.Max(textSize.X, curOffset + glyphXAdv - this.spacing);
				textSize.Y = Math.Max(textSize.Y, glyphData.Size.Y);

				curOffset += glyphXAdv;
			}

			textSize.X = MathF.Round(textSize.X);
			textSize.Y = MathF.Round(textSize.Y);
			return textSize;
		}
		/// <summary>
		/// Measures the size of a multiline text rendered using this Font.
		/// </summary>
		/// <param name="text">The text to measure.</param>
		/// <returns>The size of the measured text.</returns>
		public Vector2 MeasureText(string[] text)
		{
			if (this.texture == null) return Vector2.Zero;

			Vector2 textSize = Vector2.Zero;
			if (text == null) return textSize;

			for (int i = 0; i < text.Length; i++) {
				Vector2 lineSize = this.MeasureText(text[i]);
				textSize.X = MathF.Max(textSize.X, lineSize.X);
				textSize.Y += i == 0 ? this.fontData.Metrics.Height : MathF.RoundToInt(this.fontData.Metrics.Height * this.lineHeightFactor);
			}

			return textSize;
		}
		/// <summary>
		/// Returns a text that is cropped to fit a maximum width using this Font.
		/// </summary>
		/// <param name="text">The original text.</param>
		/// <param name="maxWidth">The maximum width it may occupy.</param>
		/// <param name="fitMode">The mode by which the text fitting algorithm operates.</param>
		/// <returns></returns>
		public string FitText(string text, float maxWidth, FitTextMode fitMode = FitTextMode.ByChar)
		{
			if (this.texture == null) return text;

			Vector2 textSize = Vector2.Zero;
			float curOffset = 0.0f;
			FontGlyphData glyphData;
			Rect uvRect;
			float glyphXAdv;
			int lastValidLength = 0;
			for (int i = 0; i < text.Length; i++) {
				this.ProcessTextAdv(text, i, out glyphData, out uvRect, out glyphXAdv);

				textSize.X = Math.Max(textSize.X, curOffset + glyphXAdv);
				textSize.Y = Math.Max(textSize.Y, glyphData.Size.Y);

				if (textSize.X > maxWidth) return lastValidLength > 0 ? text.Substring(0, lastValidLength) : "";

				if (fitMode == FitTextMode.ByChar)
					lastValidLength = i;
				else if (text[i] == ' ')
					lastValidLength = fitMode == FitTextMode.ByWordLeadingSpace ? i : i + 1;

				curOffset += glyphXAdv;
			}

			return text;
		}
		/// <summary>
		/// Measures position and size of a specific glyph inside a text.
		/// </summary>
		/// <param name="text">The text that contains the glyph to measure.</param>
		/// <param name="index">The index of the glyph to measure.</param>
		/// <returns>A rectangle that describes the specified glyphs position and size.</returns>
		public Rect MeasureTextGlyph(string text, int index)
		{
			if (this.texture == null) return Rect.Empty;

			float curOffset = 0.0f;
			FontGlyphData glyphData;
			Rect uvRect;
			float glyphXAdv;
			for (int i = 0; i < text.Length; i++) {
				this.ProcessTextAdv(text, i, out glyphData, out uvRect, out glyphXAdv);

				if (i == index) return new Rect(curOffset - glyphData.Offset.X, 0 - glyphData.Offset.Y, glyphData.Size.X, glyphData.Size.Y);

				curOffset += glyphXAdv;
			}

			return Rect.Empty;
		}

	    private void ProcessTextAdv(string text, int index, out FontGlyphData glyphData, out Rect uvRect, out float glyphXAdv)
	    {
		    char glyph = text[index];
		    int charIndex = (int)glyph > this.charLookup.Length ? 0 : this.charLookup[(int)glyph];
		    this.texture.LookupAtlas(charIndex, out uvRect);

		    this.GetGlyphData(glyph, out glyphData);

		    glyphXAdv = glyphData.Advance + this.spacing;

		    if (this.kerning) {
			    char glyphNext = index + 1 < text.Length ? text[index + 1] : ' ';
			    float advanceOffset = this.kerningLookup.GetAdvanceOffset(glyph, glyphNext);
			    glyphXAdv += advanceOffset;
		    }
	    }


        /// <summary>
        /// Renders the <see cref="Duality.Resources.FontRasterizer"/> based on its embedded TrueType representation.
        /// <param name="extendedSet">Extended set of characters for renderning.</param>
        /// </summary>
        private FontData RenderGlyphs(string fontFamily, float emSize, FontStyle style, FontCharSet extendedSet, bool antialiasing, bool monospace)
        {
            FontFamily family = new FontFamily(fontFamily);

            // Render the font's glyphs
            return this.RenderGlyphs(
                family,
                emSize,
                style,
                extendedSet,
                antialiasing,
                monospace);
        }
        /// <summary>
        /// Renders the <see cref="Duality.Resources.FontRasterizer"/> using the specified system font family.
        /// </summary>
        private FontData RenderGlyphs(FontFamily fontFamily, float emSize, FontStyle style, FontCharSet extendedSet, bool antialiasing, bool monospace)
        {
            // Determine System.Drawing font style
            SysDrawFontStyle systemStyle = SysDrawFontStyle.Regular;
            if (style.HasFlag(FontStyle.Bold)) systemStyle |= SysDrawFontStyle.Bold;
            if (style.HasFlag(FontStyle.Italic)) systemStyle |= SysDrawFontStyle.Italic;

            // Create a System.Drawing font
            SysDrawFont internalFont = null;
            if (fontFamily != null) {
                try { internalFont = new SysDrawFont(fontFamily, emSize, systemStyle); } catch (Exception e) {
                    Console.WriteLine(
                        "Failed to create System Font '{1} {2}, {3}' for rendering Duality Font glyphs: {0}",
                        /*LogFormat.Exception(*/e/*)*/,
                        fontFamily.Name,
                        emSize,
                        style);
                }
            }

            // If creating the font failed, fall back to a default one
            if (internalFont == null)
                internalFont = new SysDrawFont(FontFamily.GenericMonospace, emSize, systemStyle);

            // Render the font's glyphs
            using (internalFont) {
                return this.RenderGlyphs(
                    internalFont,
                    FontCharSet.Default.MergedWith(extendedSet),
                    antialiasing,
                    monospace);
            }
        }
        /// <summary>
        /// Renders the <see cref="Duality.Resources.FontRasterizer"/> using the specified system font.
        /// This method assumes that the system font's size and style match the one specified in
        /// the specified Duality font.
        /// </summary>
        private FontData RenderGlyphs(SysDrawFont internalFont, FontCharSet charSet, bool antialiazing, bool monospace)
        {
            FontGlyphData[] glyphs = new FontGlyphData[charSet.Chars.Length];
            for (int i = 0; i < glyphs.Length; i++) {
                glyphs[i].Glyph = charSet.Chars[i];
            }

            int bodyAscent = 0;
            int baseLine = 0;
            int descent = 0;
            int ascent = 0;

            TextRenderingHint textRenderingHint;
            if (antialiazing)
                textRenderingHint = TextRenderingHint.AntiAliasGridFit;
            else
                textRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

            int cols;
            int rows;
            cols = rows = (int)Math.Ceiling(Math.Sqrt(glyphs.Length));

            PixelData pixelLayer = new PixelData(
                MathF.RoundToInt(cols * internalFont.Size * 1.2f),
                MathF.RoundToInt(rows * internalFont.Height * 1.2f),
                ColorRgba.TransparentBlack);
            Bitmap measureBm = new Bitmap(1, 1);
            Rect[] atlas = new Rect[glyphs.Length];
            PixelData[] glyphBitmaps = new PixelData[glyphs.Length];
            using (Graphics measureGraphics = Graphics.FromImage(measureBm)) {
                Brush fntBrush = new SolidBrush(Color.Black);

                StringFormat formatDef = StringFormat.GenericDefault;
                formatDef.LineAlignment = StringAlignment.Near;
                formatDef.FormatFlags = 0;
                StringFormat formatTypo = StringFormat.GenericTypographic;
                formatTypo.LineAlignment = StringAlignment.Near;

                int x = 1;
                int y = 1;
                for (int i = 0; i < glyphs.Length; ++i) {
                    string str = glyphs[i].Glyph.ToString(CultureInfo.InvariantCulture);
                    bool isSpace = str == " ";
                    SizeF charSize = measureGraphics.MeasureString(str, internalFont, pixelLayer.Width, formatDef);

                    // Rasterize a single glyph for rendering
                    Bitmap bm = new Bitmap((int)Math.Ceiling(Math.Max(1, charSize.Width)), internalFont.Height + 1);
                    using (Graphics glyphGraphics = Graphics.FromImage(bm)) {
                        glyphGraphics.Clear(Color.Transparent);
                        glyphGraphics.TextRenderingHint = textRenderingHint;
                        glyphGraphics.DrawString(str, internalFont, fntBrush, new RectangleF(0, 0, bm.Width, bm.Height), formatDef);
                    }
                    glyphBitmaps[i] = new PixelData();
                    glyphBitmaps[i].FromBitmap(bm);

                    // Rasterize a single glyph in typographic mode for metric analysis
                    PixelData glyphTempTypo;
                    if (!isSpace) {
                        Point2 glyphTempOpaqueTopLeft;
                        Point2 glyphTempOpaqueSize;
                        glyphBitmaps[i].GetOpaqueBoundaries(out glyphTempOpaqueTopLeft, out glyphTempOpaqueSize);

                        glyphBitmaps[i].SubImage(glyphTempOpaqueTopLeft.X, 0, glyphTempOpaqueSize.X, glyphBitmaps[i].Height);

                        if (charSet.CharBodyAscentRef.Contains(glyphs[i].Glyph))
                            bodyAscent += glyphTempOpaqueSize.Y;
                        if (charSet.CharBaseLineRef.Contains(glyphs[i].Glyph))
                            baseLine += glyphTempOpaqueTopLeft.Y + glyphTempOpaqueSize.Y;
                        if (charSet.CharDescentRef.Contains(glyphs[i].Glyph))
                            descent += glyphTempOpaqueTopLeft.Y + glyphTempOpaqueSize.Y;

                        bm = new Bitmap((int)Math.Ceiling(Math.Max(1, charSize.Width)), internalFont.Height + 1);
                        using (Graphics glyphGraphics = Graphics.FromImage(bm)) {
                            glyphGraphics.Clear(Color.Transparent);
                            glyphGraphics.TextRenderingHint = textRenderingHint;
                            glyphGraphics.DrawString(str, internalFont, fntBrush, new RectangleF(0, 0, bm.Width, bm.Height), formatTypo);
                        }
                        glyphTempTypo = new PixelData();
                        glyphTempTypo.FromBitmap(bm);
                        glyphTempTypo.Crop(true, false);
                    } else {
                        glyphTempTypo = glyphBitmaps[i];
                    }

                    // Update xy values if it doesn't fit anymore
                    if (x + glyphBitmaps[i].Width + 2 > pixelLayer.Width) {
                        x = 1;
                        y += internalFont.Height + MathF.Clamp((int)MathF.Ceiling(internalFont.Height * 0.1875f), 3, 10);
                    }

                    // Memorize atlas coordinates & glyph data
                    glyphs[i].Size = glyphBitmaps[i].Size;
                    glyphs[i].Offset.X = glyphBitmaps[i].Width - glyphTempTypo.Width;
                    glyphs[i].Offset.Y = 0; // TTF fonts are rendered on blocks that are the whole size of the height - so no need for offset
                    if (isSpace) {
                        glyphs[i].Size.X /= 2;
                        glyphs[i].Offset.X /= 2;
                    }
                    glyphs[i].Advance = glyphs[i].Size.X - glyphs[i].Offset.X;

                    atlas[i].X = x;
                    atlas[i].Y = y;
                    atlas[i].W = glyphBitmaps[i].Width;
                    atlas[i].H = (internalFont.Height + 1);

                    // Draw it onto the font surface
                    glyphBitmaps[i].DrawOnto(pixelLayer, BlendMode.Solid, x, y);

                    x += glyphBitmaps[i].Width + MathF.Clamp((int)MathF.Ceiling(internalFont.Height * 0.125f), 2, 10);
                }
            }

            // White out texture except alpha channel.
            for (int i = 0; i < pixelLayer.Data.Length; i++) {
                pixelLayer.Data[i].R = 255;
                pixelLayer.Data[i].G = 255;
                pixelLayer.Data[i].B = 255;
            }

            // Monospace offset and advance adjustments
            if (monospace) {
                float maxGlyphWidth = 0;
                for (int i = 0; i < glyphs.Length; i++) {
                    maxGlyphWidth = Math.Max(maxGlyphWidth, glyphs[i].Size.X);
                }
                for (int i = 0; i < glyphs.Length; ++i) {
                    glyphs[i].Offset.X -= (int)Math.Round((maxGlyphWidth - glyphs[i].Size.X) / 2.0f);
                    glyphs[i].Advance = maxGlyphWidth;
                }
            }

            // Determine Font properties
            {
                float lineSpacing = internalFont.FontFamily.GetLineSpacing(internalFont.Style);
                float emHeight = internalFont.FontFamily.GetEmHeight(internalFont.Style);
                float cellAscent = internalFont.FontFamily.GetCellAscent(internalFont.Style);
                float cellDescent = internalFont.FontFamily.GetCellDescent(internalFont.Style);

                ascent = (int)Math.Round(cellAscent * internalFont.Size / emHeight);
                bodyAscent /= charSet.CharBodyAscentRef.Length;
                baseLine /= charSet.CharBaseLineRef.Length;
                descent = (int)Math.Round(((float)descent / charSet.CharDescentRef.Length) - (float)baseLine);
            }

            // Aggregate rendered and generated data into our return value
            FontMetrics metrics = new FontMetrics(
                size: internalFont.SizeInPoints,
                height: (int)internalFont.Height,
                ascent: ascent,
                bodyAscent: bodyAscent,
                descent: descent,
                baseLine: baseLine,
                monospace: monospace);

            // Determine kerning pairs
            FontKerningPair[] kerningPairs = null;
            if (monospace)
                kerningPairs = null;
            else
                kerningPairs = this.GatherKerningPairs(glyphs, metrics, glyphBitmaps);

            return new FontData(pixelLayer, atlas, glyphs, metrics, kerningPairs);
        }

        private FontKerningPair[] GatherKerningPairs(FontGlyphData[] glyphs, FontMetrics metrics, PixelData[] glyphBitmaps)
        {
            // Generate a sampling mask that decides at which heights we'll sample each glyph
            int[] kerningMask = this.GetKerningMask(metrics);

            // Gather samples from all glyphs that we have based on the image data we acquired
            int[][] leftSamples = new int[glyphs.Length][];
            int[][] rightSamples = new int[glyphs.Length][];
            for (int i = 0; i < glyphs.Length; i++) {
                this.GatherKerningSamples(
                    glyphs[i].Glyph,
                    glyphs[i].Offset,
                    glyphBitmaps[i],
                    kerningMask,
                    ref leftSamples[i],
                    ref rightSamples[i]);
            }

            // Find all glyph combinations with a non-zero kerning offset
            List<FontKerningPair> pairs = new List<FontKerningPair>();
            for (int i = 0; i < glyphs.Length; i++) {
                for (int j = 0; j < glyphs.Length; j++) {
                    // Calculate the smallest depth sum across all height samples
                    int minSum = int.MaxValue;
                    for (int k = 0; k < rightSamples[i].Length; k++)
                        minSum = Math.Min(minSum, rightSamples[i][k] + leftSamples[j][k]);

                    // The smallest one represents the amount of pixels between the two
                    // glyphs that is completely empty. Out kerning offset will be the negative
                    // of that to make the two glyphs appear closer together.
                    float kerningOffset = -minSum;
                    if (kerningOffset != 0.0f) {
                        pairs.Add(new FontKerningPair(
                            glyphs[i].Glyph,
                            glyphs[j].Glyph,
                            kerningOffset));
                    }
                }
            }

            return pairs.ToArray();
        }
        private int[] GetKerningMask(FontMetrics metrics)
        {
            int kerningSamples = (metrics.Ascent + metrics.Descent) / 4;
            int[] kerningY;
            if (kerningSamples <= 6) {
                kerningSamples = 6;
                kerningY = new int[] {
                    metrics.BaseLine - metrics.Ascent,
                    metrics.BaseLine - metrics.BodyAscent,
                    metrics.BaseLine - metrics.BodyAscent * 2 / 3,
                    metrics.BaseLine - metrics.BodyAscent / 3,
                    metrics.BaseLine,
                    metrics.BaseLine + metrics.Descent};
            } else {
                kerningY = new int[kerningSamples];
                int bodySamples = kerningSamples * 2 / 3;
                int descentSamples = (kerningSamples - bodySamples) / 2;
                int ascentSamples = kerningSamples - bodySamples - descentSamples;

                for (int k = 0; k < ascentSamples; k++)
                    kerningY[k] = metrics.BaseLine - metrics.Ascent + k * (metrics.Ascent - metrics.BodyAscent) / ascentSamples;
                for (int k = 0; k < bodySamples; k++)
                    kerningY[ascentSamples + k] = metrics.BaseLine - metrics.BodyAscent + k * metrics.BodyAscent / (bodySamples - 1);
                for (int k = 0; k < descentSamples; k++)
                    kerningY[ascentSamples + bodySamples + k] = metrics.BaseLine + (k + 1) * metrics.Descent / descentSamples;
            }
            return kerningY;
        }
        private void GatherKerningSamples(char glyph, Vector2 glyphOffset, PixelData glyphBitmap, int[] sampleMask, ref int[] samplesLeft, ref int[] samplesRight)
        {
            samplesLeft = new int[sampleMask.Length];
            samplesRight = new int[sampleMask.Length];

            if (glyph == ' ') return;
            if (glyph == '\t') return;
            if (glyphBitmap.Width <= 0) return;
            if (glyphBitmap.Height <= 0) return;

            Point2 glyphSize = glyphBitmap.Size;

            // Left side samples
            {
                int leftMid = glyphSize.X / 2;
                int lastSampleY = 0;
                for (int sampleIndex = 0; sampleIndex < samplesLeft.Length; sampleIndex++) {
                    samplesLeft[sampleIndex] = leftMid;

                    int sampleY = sampleMask[sampleIndex] + (int)glyphOffset.Y;
                    int beginY = MathF.Clamp(lastSampleY, 0, glyphSize.Y - 1);
                    int endY = MathF.Clamp(sampleY, 0, glyphSize.Y);
                    if (sampleIndex == samplesLeft.Length - 1) endY = glyphSize.Y;
                    lastSampleY = endY;

                    for (int y = beginY; y < endY; y++) {
                        int x = 0;
                        while (glyphBitmap[x, y].A <= 64) {
                            x++;
                            if (x >= leftMid) break;
                        }
                        samplesLeft[sampleIndex] = Math.Min(samplesLeft[sampleIndex], x);
                    }
                }
            }

            // Right side samples
            {
                int rightMid = (glyphSize.X + 1) / 2;
                int lastSampleY = 0;
                for (int sampleIndex = 0; sampleIndex < samplesRight.Length; sampleIndex++) {
                    samplesRight[sampleIndex] = rightMid;

                    int sampleY = sampleMask[sampleIndex] + (int)glyphOffset.Y;
                    int beginY = MathF.Clamp(lastSampleY, 0, glyphSize.Y - 1);
                    int endY = MathF.Clamp(sampleY, 0, glyphSize.Y);
                    if (sampleIndex == samplesRight.Length - 1) endY = glyphSize.Y;
                    lastSampleY = endY;

                    for (int y = beginY; y < endY; y++) {
                        int x = glyphSize.X - 1;
                        while (glyphBitmap[x, y].A <= 64) {
                            x--;
                            if (x <= rightMid) break;
                        }
                        samplesRight[sampleIndex] = Math.Min(samplesRight[sampleIndex], glyphSize.X - 1 - x);
                    }
                }
            }

            return;
        }
    }
}