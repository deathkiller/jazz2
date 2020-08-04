using System.Runtime.CompilerServices;
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
            if (!InnerView.ShowTouchButtons || InnerView.TouchButtons == null) {
                return;
            }

            canvas.State.ColorTint = ColorRgba.White;

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