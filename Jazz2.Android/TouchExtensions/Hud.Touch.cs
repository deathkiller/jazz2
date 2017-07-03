using Duality;
using Duality.Drawing;
using Jazz2.Android;

namespace Jazz2.Game
{
    partial class Hud
    {
        partial void DrawTouch(IDrawDevice device, Canvas c, Vector2 size)
        {
            if (!GLView.showVirtualButtons || GLView.virtualButtons == null) {
                return;
            }

            c.State.ColorTint = ColorRgba.White;

            for (int i = 0; i < GLView.virtualButtons.Length; i++) {
                GLView.VirtualButton button = GLView.virtualButtons[i];
                if (button.Material.IsAvailable) {
                    c.State.SetMaterial(button.Material);
                    c.FillOval(button.Left * size.X, button.Top * size.Y, button.Width * size.X, button.Height * size.Y);
                    c.DrawOval(button.Left * size.X, button.Top * size.Y, button.Width * size.X, button.Height * size.Y);
                }
            }
        }
    }
}