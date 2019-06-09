
using Duality.Drawing;

using OpenTK.Graphics.OpenGL;

namespace Duality.Backend.GL21
{
    public static class ExtMethodColorDataLayout
	{
		public static PixelFormat ToOpenTK(this ColorDataLayout layout)
		{
			switch (layout)
			{
				default:
				case ColorDataLayout.Rgba: return PixelFormat.Rgba;
			}
		}
	}
}
