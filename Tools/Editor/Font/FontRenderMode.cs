namespace Duality.Resources
{
    /// <summary>
    /// Specifies how a Font is rendered. This affects both internal glyph rasterization and rendering.
    /// </summary>
    public enum FontRenderMode
	{
		/// <summary>
		/// A monochrome bitmap is used to store glyphs. Rendering is unfiltered and pixel-perfect.
		/// </summary>
		MonochromeBitmap,
		/// <summary>
		/// A greyscale bitmap is used to store glyphs. Rendering is unfiltered and pixel-perfect.
		/// </summary>
		GrayscaleBitmap,
		/// <summary>
		/// A greyscale bitmap is used to store glyphs. Rendering is properly filtered but may blur text display a little.
		/// </summary>
		SmoothBitmap,
		/// <summary>
		/// A greyscale bitmap is used to store glyphs. Rendering is properly filtered and uses a shader to enforce sharp masked edges.
		/// </summary>
		SharpBitmap,

        ClearType
	}
}