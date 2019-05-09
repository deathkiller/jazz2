using System;
using Duality;
using Duality.Drawing;
using Duality.Input;

namespace Jazz2.Game.UI.Menu
{
    public class AboutSection : MenuSection
    {
        private string libopenmptText;

        public AboutSection()
        {
            Version libopenmptVersion = OpenMptStream.Version;
            if (libopenmptVersion != null) {
                libopenmptText = "Uses libopenmpt library (v\f[w:75]" + libopenmptVersion + "\f[w:100]) released under BSD license.";
            }
        }

        public override void OnPaint(Canvas canvas, Rect view)
        {
            IDrawDevice device = canvas.DrawDevice;

            Vector2 size = device.TargetSize;

            Vector2 pos = size * 0.5f;
            pos.Y = Math.Max(150f, pos.Y * 0.75f);

            api.DrawMaterial("MenuDim", pos.X, pos.Y + 60f - 2f, Alignment.Top, ColorRgba.White, 55f, 14.2f, new Rect(0f, 0.3f, 1f, 0.7f));

            pos.X *= 0.35f;

            int charOffset = 0;

            api.DrawStringShadow(ref charOffset, "menu/about/desc".T(), pos.X, pos.Y - 22f,
                Alignment.Left, ColorRgba.TransparentBlack, 0.7f, 0.4f, 0.6f, 0.6f, 7f, 0.9f, 1.2f);

            api.DrawStringShadow(ref charOffset, "menu/about/by".T(), pos.X, pos.Y + 20f,
                Alignment.Left, ColorRgba.TransparentBlack, 0.85f, 0.4f, 0.6f, 0.6f, 7f, 0.9f);
            api.DrawStringShadow(ref charOffset, "Dan R.", pos.X + 25f, pos.Y + 20f + 20f,
                Alignment.Left, ColorRgba.TransparentBlack, 1.0f, 0.4f, 0.75f, 0.75f, 7f, 0.9f);

            api.DrawStringShadow(ref charOffset, "<https://github.com/deathkiller/jazz2>", pos.X + 25f + 70f, pos.Y + 20f + 20f,
                Alignment.Left, ColorRgba.TransparentBlack, 0.7f, 0.4f, 0.6f, 0.6f, 7f, 0.9f);

            float y = pos.Y + 80f;

            api.DrawStringShadow(ref charOffset, "Uses parts of Duality - A 2D GameDev Framework released under MIT/X11 license.", pos.X, y,
                Alignment.Left, ColorRgba.TransparentBlack, 0.7f, 0.4f, 0.6f, 0.6f, 7f, 0.9f);
            api.DrawStringShadow(ref charOffset, "Fedja Adam & contributors", pos.X + 25f, y + 16f,
                Alignment.Left, ColorRgba.TransparentBlack, 0.8f, 0.4f, 0.7f, 0.7f, 7f, 0.9f);

            y += 40f;

            api.DrawStringShadow(ref charOffset, "Uses parts of Project Carrot released under MIT/X11 license.", pos.X, y,
                Alignment.Left, ColorRgba.TransparentBlack, 0.7f, 0.4f, 0.6f, 0.6f, 7f, 0.9f);
            api.DrawStringShadow( ref charOffset, "Soulweaver  <soulweaver@hotmail.fi>", pos.X + 25f, y + 16f,
                Alignment.Left, ColorRgba.TransparentBlack, 0.8f, 0.4f, 0.7f, 0.7f, 7f, 0.9f);

            y += 40f;

            if (size.Y - 50f > y && libopenmptText != null) {
                api.DrawStringShadow(ref charOffset, libopenmptText, pos.X, y,
                    Alignment.Left, ColorRgba.TransparentBlack, 0.7f, 0.4f, 0.6f, 0.6f, 7f, 0.9f);
                api.DrawStringShadow(ref charOffset, "Olivier Lapicque & OpenMPT contributors", pos.X + 25f, y + 16f,
                    Alignment.Left, ColorRgba.TransparentBlack, 0.8f, 0.4f, 0.7f, 0.7f, 7f, 0.9f);

                y += 40f;
            }

            if (size.Y - 50f > y) {
                api.DrawStringShadow(ref charOffset, "Uses OpenTK library released under MIT/X11 license.", pos.X, y,
                    Alignment.Left, ColorRgba.TransparentBlack, 0.7f, 0.4f, 0.6f, 0.6f, 7f, 0.9f);
                api.DrawStringShadow(ref charOffset, "Stefanos Apostolopoulos  <stapostol@gmail.com>", pos.X + 25f, y + 16f,
                    Alignment.Left, ColorRgba.TransparentBlack, 0.8f, 0.4f, 0.7f, 0.7f, 7f, 0.9f);
            }

            api.DrawMaterial("MenuLine", 0, /*center.X*/device.TargetSize.X * 0.5f, pos.Y + 60f, Alignment.Center, ColorRgba.White, 1.6f);
        }

        public override void OnUpdate()
        {
            if (ControlScheme.MenuActionHit(PlayerActions.Menu)) {
                api.PlaySound("MenuSelect", 0.5f);
                api.LeaveSection(this);
            }
        }
    }
}