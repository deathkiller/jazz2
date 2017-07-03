using System;
using Duality;
using Duality.Drawing;
using Duality.Input;
using static Jazz2.Settings;

namespace Jazz2.Game.Menu.S
{
    public class SettingsSection : MainMenuSection
    {
        private MenuControlBase[] controls;

        private int selectedIndex;
        private float animation;

        public SettingsSection()
        {
        }

        public override void OnShow(MainMenu root)
        {
            animation = 0f;

            base.OnShow(root);

            controls = new MenuControlBase[] {
                new ChoiceControl(api, "Resize Mode", (int)Resize, "None", "HQ2x", "3xBRZ"),
                //new LinkControl(api, "Controls", () => {}),
            };
        }

        public override void OnPaint(IDrawDevice device, Canvas c)
        {
            Vector2 center = device.TargetSize * 0.5f;
            center.Y *= 0.8f;

            int charOffset = 0;
            for (int i = 0; i < controls.Length; i++) {
                controls[i].OnDraw(device, c, ref center, selectedIndex == i);
            }

#if __ANDROID__
            var fs = (DualityApp.SystemBackend.FileSystem as Duality.Backend.Android.NativeFileSystem);
            if (fs != null) {
                api.DrawMaterial(c, "MenuSettingsStorage", 180f, center.Y + 10f - 3f, Alignment.Right, ColorRgba.White);

                api.DrawStringShadow(device, ref charOffset, "Content Path:",
                    180f + 10f, center.Y + 10f, Alignment.Left, new ColorRgba(0.68f, 0.46f, 0.42f, 0.5f), 0.8f, charSpacing: 0.9f);

                api.DrawString(device, ref charOffset, fs.RootPath,
                    180f + 10f + 98f, center.Y + 10f, Alignment.Left, new ColorRgba(0.46f, 0.5f), 0.8f, charSpacing: 0.85f);
            }
#else
            api.DrawString(device, ref charOffset,
                "Controls\nArrows = Move\nV = Jump\nSpace = Fire\nC = Run\nX = Switch Weapon",
                center.X, center.Y + 40f, Alignment.Top, ColorRgba.TransparentBlack, 0.82f, charSpacing: 0.9f);
#endif
        }

        public override void OnUpdate()
        {
            if (animation < 1f) {
                animation = Math.Min(animation + Time.TimeMult * 0.016f, 1f);
            }

            controls[selectedIndex].OnUpdate();

            if (!controls[selectedIndex].IsInputCaptured) {
                if (DualityApp.Keyboard.KeyHit(Key.Enter)) {
                    //
                } else if (DualityApp.Keyboard.KeyHit(Key.Up)) {
                    api.PlaySound("MenuSelect", 0.4f);
                    animation = 0f;
                    if (selectedIndex > 0) {
                        selectedIndex--;
                    } else {
                        selectedIndex = controls.Length - 1;
                    }
                } else if (DualityApp.Keyboard.KeyHit(Key.Down)) {
                    api.PlaySound("MenuSelect", 0.4f);
                    animation = 0f;
                    if (selectedIndex < controls.Length - 1) {
                        selectedIndex++;
                    } else {
                        selectedIndex = 0;
                    }
                } else if (DualityApp.Keyboard.KeyHit(Key.Escape)) {
                    Commit();

                    api.PlaySound("MenuSelect", 0.5f);
                    api.LeaveSection(this);
                }
            }
        }

        private void Commit()
        {
            Resize = (ResizeMode)((ChoiceControl)controls[0]).SelectedIndex;
        }
    }
}