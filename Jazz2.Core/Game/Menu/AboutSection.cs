using Duality;
using Duality.Drawing;
using Duality.Input;

namespace Jazz2.Game.Menu
{
    public class AboutSection : MainMenuSection
    {

        public override void OnPaint(IDrawDevice device, Canvas c)
        {
            Vector2 center = device.TargetSize * 0.5f;
            center.Y *= 0.5f + 0.2f;

            api.DrawMaterial(c, "MenuDim", center.X, ((center.Y + 60f) + 420f) * 0.5f, Alignment.Center, ColorRgba.White, 80f, 27f);

            center.X *= 0.35f;

            int charOffset = 0;

            api.DrawStringShadow(device, ref charOffset, "Remake of game Jazz Jackrabbit 2 from year 1998. Supports various\nversions of the game (Shareware Demo, Holiday Hare '98, The Secret Files\nand Christmas Chronicles). Also, it partially supports JJ2+ extension.", center.X, center.Y - 22f,
                Alignment.Left, ColorRgba.TransparentBlack, 0.7f, 0.4f, 0.6f, 0.6f, 7f, 0.9f, 1.2f);

            api.DrawStringShadow(device, ref charOffset, "Created By", center.X, center.Y + 20f,
                Alignment.Left, ColorRgba.TransparentBlack, 0.85f, 0.4f, 0.6f, 0.6f, 7f, 0.9f);
            api.DrawStringShadow(device, ref charOffset, "Dan R.", center.X + 25f, center.Y + 20f + 20f,
                Alignment.Left, ColorRgba.TransparentBlack, 1.0f, 0.4f, 0.75f, 0.75f, 7f, 0.9f);

            api.DrawStringShadow(device, ref charOffset, "(http://deat.tk/)", center.X + 25f + 70f, center.Y + 20f + 20f,
                Alignment.Left, ColorRgba.TransparentBlack, 0.7f, 0.4f, 0.6f, 0.6f, 7f, 0.8f);


            api.DrawStringShadow(device, ref charOffset, "Uses parts of Duality Engine released under MIT/X11 license.", center.X, center.Y + 80f,
                Alignment.Left, ColorRgba.TransparentBlack, 0.7f, 0.4f, 0.6f, 0.6f, 7f, 0.9f);
            api.DrawStringShadow(device, ref charOffset, "Fedja Adam", center.X + 25f, center.Y + 80f + 16f,
                Alignment.Left, ColorRgba.TransparentBlack, 0.8f, 0.4f, 0.7f, 0.7f, 7f, 0.9f);

            api.DrawStringShadow(device, ref charOffset, "Uses parts of Project Carrot released under MIT/X11 license.", center.X, center.Y + 120f,
                Alignment.Left, ColorRgba.TransparentBlack, 0.7f, 0.4f, 0.6f, 0.6f, 7f, 0.9f);
            api.DrawStringShadow(device, ref charOffset, "Soulweaver  <soulweaver@hotmail.fi>", center.X + 25f, center.Y + 120f + 16f,
                Alignment.Left, ColorRgba.TransparentBlack, 0.8f, 0.4f, 0.7f, 0.7f, 7f, 0.9f);

            api.DrawStringShadow(device, ref charOffset, "Uses libopenmpt library released under BSD license.", center.X, center.Y + 160f,
                Alignment.Left, ColorRgba.TransparentBlack, 0.7f, 0.4f, 0.6f, 0.6f, 7f, 0.9f);
            api.DrawStringShadow(device, ref charOffset, "Olivier Lapicque & OpenMPT contributors", center.X + 25f, center.Y + 160f + 16f,
                Alignment.Left, ColorRgba.TransparentBlack, 0.8f, 0.4f, 0.7f, 0.7f, 7f, 0.9f);

            api.DrawStringShadow(device, ref charOffset, "Uses OpenTK library released under MIT/X11 license.", center.X, center.Y + 200f,
                Alignment.Left, ColorRgba.TransparentBlack, 0.7f, 0.4f, 0.6f, 0.6f, 7f, 0.9f);
            api.DrawStringShadow(device, ref charOffset, "Stefanos Apostolopoulos  <stapostol@gmail.com>", center.X + 25f, center.Y + 200f + 16f,
                Alignment.Left, ColorRgba.TransparentBlack, 0.8f, 0.4f, 0.7f, 0.7f, 7f, 0.9f);


            api.DrawMaterial(c, "MenuLineTop", /*center.X*/device.TargetSize.X * 0.5f, center.Y + 60f, Alignment.Center, ColorRgba.White, 1.6f);
        }

        public override void OnUpdate()
        {
            if (DualityApp.Keyboard.KeyHit(Key.Escape)) {
                api.PlaySound("MenuSelect", 0.5f);
                api.LeaveSection(this);
            }
        }
    }
}