using Duality;
using Duality.Drawing;

namespace Jazz2.Game.UI.Menu
{
    public class ReinstallNeededSection : MenuSection
    {
        public ReinstallNeededSection()
        {
        }

        public override void OnPaint(Canvas canvas, Rect view)
        {
            IDrawDevice device = canvas.DrawDevice;

            Vector2 center = device.TargetSize * 0.5f;

            int charOffset = 0;
            api.DrawString(ref charOffset, "Installation is not complete!\n", center.X, center.Y - 50, Alignment.Center,
                    new ColorRgba(0.5f, 0.32f, 0.32f, 0.5f), 1f, 0.4f, 0.6f, 0.6f, 8f, charSpacing: 0.86f);

            api.DrawString(ref charOffset, "\f[c:6]Run \f[c:2]Import.exe\f[c:6] first to import game files\nfrom your version of the original game.\nShareware Demo can also be imported.\n\nVisit \f[c:3]http://deat.tk/jazz2/\f[c:6] for more info!", center.X, center.Y - 10, Alignment.Top,
                    ColorRgba.White, 0.9f, 0.4f, 0.6f, 0.6f, 8f, charSpacing: 0.88f);

            charOffset = 2;
#if PLATFORM_ANDROID
            api.DrawString(ref charOffset, "\f[c:6]Press \f[c:-1]Back\f[c:6] to exit", center.X, center.Y + 110, Alignment.Center,
                ColorRgba.White, 0.8f, 0.4f, 0.6f, 0.6f, 8f, charSpacing: 0.88f);
#else
            api.DrawString(ref charOffset, "\f[c:6]Press \f[c:-1]Escape\f[c:6] to exit", center.X, center.Y + 110, Alignment.Center,
                ColorRgba.White, 0.8f, 0.4f, 0.6f, 0.6f, 8f, charSpacing: 0.88f);
#endif
        }

        public override void OnUpdate()
        {
            if (ControlScheme.MenuActionHit(PlayerActions.Menu)) {
                DualityApp.Terminate();
            }
        }
    }
}