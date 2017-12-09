using Duality.Drawing;
using OpenTK.Graphics.ES20;

namespace Duality.Backend.Es20
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