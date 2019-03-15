using Android.App;
using Android.Content.Res;
using Duality;
using Duality.Drawing;
using Duality.Resources;
using Jazz2.Android;

namespace Jazz2.Game.UI.Menu
{
    partial class MainMenu
    {
        private int statusBarHeight;

        partial void InitPlatformSpecific()
        {
            Resources res = Application.Context.Resources;
            int resId = res.GetIdentifier("status_bar_height", "dimen", "android");
            if (resId > 0) {
                statusBarHeight = res.GetDimensionPixelSize(resId);
            }
        }

        partial void DrawPlatformSpecific(Vector2 size)
        {
            int y = (statusBarHeight * (int)size.Y / DualityApp.WindowSize.Y);

            canvas.State.SetMaterial(DrawTechnique.Alpha);

            canvas.State.ColorTint = new ColorRgba(0f, 0.2f);
            canvas.FillRect(0, 0, size.X, y);

            canvas.State.ColorTint = new ColorRgba(0.9f, 0.5f);
            canvas.DrawLine(0, y, size.X, y);

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