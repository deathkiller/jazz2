using Duality;
using Duality.Drawing;
using Jazz2.Android;

namespace Jazz2.Game.UI.Menu.InGame
{
    partial class InGameMenu
    {
        partial void DrawPlatformSpecific(Vector2 size)
        {
#if ENABLE_TOUCH
            if (!InnerView.showVirtualButtons || InnerView.virtualButtons == null) {
                return;
            }

            canvas.State.ColorTint = ColorRgba.White;

            for (int i = 0; i < InnerView.virtualButtons.Length; i++) {
                ref InnerView.VirtualButton button = ref InnerView.virtualButtons[i];
                if (button.Material.IsAvailable) {
                    canvas.State.SetMaterial(button.Material);
                    canvas.FillRect(button.Left * size.X, button.Top * size.Y, button.Width * size.X, button.Height * size.Y);
                }
            }
#endif
        }
    }
}