using Duality;
using Duality.Android;
using Duality.Drawing;

namespace Jazz2.Game.UI
{
    partial class Hud
    {
        partial void DrawTouch(IDrawDevice device, Canvas c, Vector2 size)
        {
            if (!InnerView.showVirtualButtons || InnerView.virtualButtons == null) {
                return;
            }

            c.State.ColorTint = ColorRgba.White;

            for (int i = 0; i < InnerView.virtualButtons.Length; i++) {
                InnerView.VirtualButton button = InnerView.virtualButtons[i];
                if (button.Material.IsAvailable) {
                    c.State.SetMaterial(button.Material);
                    c.FillOval(button.Left * size.X, button.Top * size.Y, button.Width * size.X, button.Height * size.Y);
                    c.DrawOval(button.Left * size.X, button.Top * size.Y, button.Width * size.X, button.Height * size.Y);
                }
            }
        }
    }
}