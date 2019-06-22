using Duality.Drawing;
using OpenTK.Graphics.ES30;

namespace Duality.Backend.Android.OpenTK
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