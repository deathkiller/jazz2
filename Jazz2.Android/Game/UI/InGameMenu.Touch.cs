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