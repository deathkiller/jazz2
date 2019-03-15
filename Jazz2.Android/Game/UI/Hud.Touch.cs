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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        partial void AdjustVisibleZone(ref Rect view)
        {
#if ENABLE_TOUCH
            if (!InnerView.showVirtualButtons || InnerView.virtualButtons == null) {
                return;
            }

            view.X = 100;
            view.W = view.W - 100 - 160;
#endif
        }
    }
}