
using Duality.Drawing;

namespace Duality.Resources
{
    /// <summary>
    /// Represents a block of bitmap <see cref="FontRasterizer"/> data.
    /// </summary>
    public class FontData
	{
		private FontGlyphData[]   glyphs       = null;
		private FontKerningPair[] kerningPairs = null;
		private Rect[]            atlas        = null;
		private PixelData         bitmap       = null;
		private FontMetrics       metrics      = null;


        /// <summary>
        /// [GET] Data about the glyphs that are supported by this bitmap <see cref="FontRasterizer"/>, including the metrics 
        /// that are required to write a text using them.
        /// </summary>
        public FontGlyphData[] Glyphs
		{
			get { return this.glyphs; }
		}
		/// <summary>
		/// [GET] An optional array of kerning pairs that represent deviations from the default spacing for certain
		/// pairs of glyphs when occurring next to each other.
		/// </summary>
		public FontKerningPair[] KerningPairs
		{
			get { return this.kerningPairs; }
		}
		/// <summary>
		/// [GET] A block of pixel data that contains the visual representation for all supported glyphs.
		/// </summary>
		public PixelData Bitmap
		{
			get { return this.bitmap; }
		}
		/// <summary>
		/// [GET] An atlas that allows to address each glyph's visual representation inside the <see cref="Bitmap"/> of this font.
		/// </summary>
		public Rect[] Atlas
		{
			get { return this.atlas; }
		}
		/// <summary>
		/// [GET] Provides access to various metrics that are inherent to the represented font, such as size, height and other
		/// typographic values.
		/// </summary>
		public FontMetrics Metrics
		{
			get { return this.metrics; }
		}


		public FontData(PixelData bitmap, Rect[] atlas, FontGlyphData[] glyphs, FontMetrics metrics, FontKerningPair[] kerningPairs)
		{
			this.bitmap = bitmap;
			this.atlas = atlas;
			this.glyphs = glyphs;
			this.metrics = metrics;
			this.kerningPairs = kerningPairs;
		}
	}
}