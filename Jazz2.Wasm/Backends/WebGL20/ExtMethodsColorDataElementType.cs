using Duality.Drawing;
using WebGLDotNET;

namespace Duality.Backend.Android.OpenTK
{
	public static class ExtMethodColorDataElementType
	{
		public static uint ToOpenTK(this ColorDataElementType type)
		{
			switch (type)
			{
				default:
				case ColorDataElementType.Byte: return WebGLRenderingContextBase.UNSIGNED_BYTE;
				case ColorDataElementType.Float: return WebGLRenderingContextBase.FLOAT;
			}
		}
	}
}