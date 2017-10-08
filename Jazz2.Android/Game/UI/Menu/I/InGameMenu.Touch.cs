using Duality;
using Duality.Android;
using Duality.Drawing;

namespace Jazz2.Game.UI.Menu.I
{
    partial class InGameMenu
    {
        partial void InitTouch()
        {
        }

        partial void DrawTouch(Vector2 size)
        {
            if (!InnerView.showVirtualButtons || InnerView.virtualButtons == null) {
                return;
            }

            canvas.State.ColorTint = ColorRgba.White;

            for (int i = 0; i < InnerView.virtualButtons.Length; i++) {
                InnerView.VirtualButton button = InnerView.virtualButtons[i];
                if (button.Material.IsAvailable) {
                    canvas.State.SetMaterial(button.Material);
                    canvas.FillOval(button.Left * size.X, button.Top * size.Y, button.Width * size.X, button.Height * size.Y);
                    canvas.DrawOval(button.Left * size.X, button.Top * size.Y, button.Width * size.X, button.Height * size.Y);
                }
            }
        }
    }
}