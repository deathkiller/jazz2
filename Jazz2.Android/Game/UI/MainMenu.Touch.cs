using System.Runtime.CompilerServices;
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
            if (!InnerView.ShowTouchButtons || InnerView.TouchButtons == null) {
                return;
            }

            canvas.State.ColorTint = ColorRgba.White;

            for (int i = 0; i < InnerView.TouchButtons.Length; i++) {
                ref InnerView.TouchButtonInfo button = ref InnerView.TouchButtons[i];
                if (button.Material.IsAvailable) {
                    float x = button.Left;
                    if (x < 0.5f) {
                        x += InnerView.LeftPadding;
                    } else {
                        x -= InnerView.RightPadding;
                    }

                    canvas.State.SetMaterial(button.Material);
                    canvas.FillRect(x * size.X, button.Top * size.Y, button.Width * size.X, button.Height * size.Y);
                }
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        partial void AdjustVisibleZone(ref Rect view)
        {
#if ENABLE_TOUCH
            if (!InnerView.ShowTouchButtons || InnerView.TouchButtons == null)
            {
                return;
            }

            float width = view.W;

            view.X = InnerView.LeftPadding * width;
            view.W = view.W - view.X - (InnerView.RightPadding * width);
#endif
        }
    }
}