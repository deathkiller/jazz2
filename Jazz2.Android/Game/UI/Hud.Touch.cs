using Duality;
using Duality.Drawing;
using Jazz2.Android;
using System.Runtime.CompilerServices;

namespace Jazz2.Game.UI
{
    partial class Hud
    {
        partial void DrawPlatformSpecific(Vector2 size)
        {
#if !DEBUG
            //fontSmall.DrawString(ref charOffset, Time.Fps.ToString(), 2, 2, Alignment.TopLeft, ColorRgba.TransparentBlack, 0.8f);
#endif

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
            if (!InnerView.ShowTouchButtons || InnerView.TouchButtons == null) {
                return;
            }

            float width = view.W;

            view.X = 90 + InnerView.LeftPadding * width;
            view.W = view.W - view.X - (140 + InnerView.RightPadding * width);
#endif
        }
    }
}