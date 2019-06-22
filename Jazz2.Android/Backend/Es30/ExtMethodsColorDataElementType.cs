using Duality.Drawing;
using OpenTK.Graphics.ES30;

namespace Duality.Backend.Android.OpenTK
{
    public static class ExtMethodColorDataElementType
	{
		public static PixelType ToOpenTK(this ColorDataElementType type)
		{
			switch (type)
			{
				default:
				case ColorDataElementType.Byte: return PixelType.UnsignedByte;
				case ColorDataElementType.Float: return PixelType.Float;
			}
		}
	}
}