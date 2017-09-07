using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Import
{
    public static class ExtensionMethods
    {
        public static void DrawImageEx(this Graphics g, Image i, RectangleF r, byte alpha, bool grayscaled)
        {
            if (alpha == 0)
                return;

            PixelOffsetMode oldPOM = g.PixelOffsetMode;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            PointF ulCorner = new PointF(r.Left, r.Top);
            PointF urCorner = new PointF(r.Right, r.Top);
            PointF llCorner = new PointF(r.Left, r.Bottom);
            PointF[] destPoints = { ulCorner, urCorner, llCorner };

            if (alpha == 0xff && !grayscaled) {
                g.DrawImage(i, destPoints, new RectangleF(0, 0, i.Width, i.Height), GraphicsUnit.Pixel);
                return;
            }

            ColorMatrix colorMatrix;
            if (grayscaled) {
                colorMatrix = new ColorMatrix(new[] {
                    new float[] { 0.299f, 0.299f, 0.299f, 0, 0 },
                    new float[] { 0.587f, 0.587f, 0.587f, 0, 0 },
                    new float[] { 0.114f, 0.114f, 0.114f, 0, 0 },
                    new float[] { 0, 0, 0, alpha / 255f, 0 },
                    new float[] { 0, 0, 0, 0, 1 }
                });
            } else {
                colorMatrix = new ColorMatrix(new[] {
                    new float[] { 1, 0, 0, 0, 0 },
                    new float[] { 0, 1, 0, 0, 0 },
                    new float[] { 0, 0, 1, 0, 0 },
                    new float[] { 0, 0, 0, alpha / 255f, 0 },
                    new float[] { 0, 0, 0, 0, 1 }
                });
            }
            using (ImageAttributes imageAttributes = new ImageAttributes()) {
                imageAttributes.SetColorMatrix(colorMatrix);
                g.DrawImage(i, destPoints, new RectangleF(0, 0, i.Width, i.Height), GraphicsUnit.Pixel, imageAttributes);
            }
            g.PixelOffsetMode = oldPOM;
        }
    }
}