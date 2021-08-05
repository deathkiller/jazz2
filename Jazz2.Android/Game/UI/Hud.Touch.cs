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

            canvas.State.ColorTint = ColorRgba.White.WithAlpha(InnerView.ControlsOpacity);

            for (int i = 0; i < InnerView.TouchButtons.Length; i++) {
                ref InnerView.TouchButtonInfo button = ref InnerView.TouchButtons[i];
                if (button.Material.IsAvailable) {
                    float x = button.Left;
                    float y = button.Top;
                    if (x < 0.5f) {
                        x += InnerView.LeftPadding;
                        y += InnerView.BottomPadding1;
                    } else {
                        x -= InnerView.RightPadding;
                        y += InnerView.BottomPadding2;
                    }

                    canvas.State.SetMaterial(button.Material);
                    canvas.FillRect(x * size.X, y * size.Y, button.Width * size.X, button.Height * size.Y);
                }
            }

            canvas.State.ColorTint = ColorRgba.White;
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