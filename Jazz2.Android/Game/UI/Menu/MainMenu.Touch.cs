using Android.App;
using Android.Content.Res;
using Duality;
using Duality.Android;
using Duality.Drawing;
using Duality.Resources;

namespace Jazz2.Game.UI.Menu
{
    partial class MainMenu
    {
        private int statusBarHeight;

        partial void InitTouch()
        {
            Resources res = Application.Context.Resources;
            int resId = res.GetIdentifier("status_bar_height", "dimen", "android");
            if (resId > 0) {
                statusBarHeight = res.GetDimensionPixelSize(resId);
            }
        }

        partial void DrawTouch(IDrawDevice device, Canvas c, Vector2 size)
        {
            int y = (statusBarHeight * (int)size.Y / DualityApp.WindowSize.Y);
            c.State.SetMaterial(new Material(DrawTechnique.Alpha, new ColorRgba(0.9f, 0.4f)));
            c.State.ColorTint = ColorRgba.White;
            c.DrawLine(0, y, size.X, y);

            if (!InnerView.showVirtualButtons || InnerView.virtualButtons == null) {
                return;
            }

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