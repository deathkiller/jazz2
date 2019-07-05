using Duality.Drawing;
using WebGLDotNET;

namespace Duality.Backend.Wasm
{
    public static class ExtMethodColorDataLayout
    {
        public static uint ToOpenTK(this ColorDataLayout layout)
        {
            switch (layout)
            {
                default:
                case ColorDataLayout.Rgba: return WebGLRenderingContextBase.RGBA;
            }
        }
    }
}