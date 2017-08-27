using Duality;
using Duality.Drawing;
using Jazz2.Storage;
using static Jazz2.Settings;

namespace Jazz2.Game.UI.Menu.S
{
    public class SettingsSection : MainMenuSectionWithControls
    {
        public override void OnShow(MainMenu root)
        {
            base.OnShow(root);

            controls = new MenuControlBase[] {
                // 3xBRZ shader is not available in OpenGL ES 3.0 version
#if __ANDROID__
                new ChoiceControl(api, "Resize Mode", (int)Resize, "None", "HQ2x"),
                new ChoiceControl(api, "Vibrations", Duality.Android.InnerView.allowVibrations ? 1 : 0, "Disable", "Enable"),
#else
                new ChoiceControl(api, "Resize Mode", (int)Resize, "None", "HQ2x", "3xBRZ"),
#endif
                new LinkControl(api, "Controls", OnControlsPressed)
            };
        }

        public override void OnHide(bool isRemoved)
        {
            if (isRemoved) {
                Commit();
            }

            base.OnHide(isRemoved);
        }

        public override void OnPaint(IDrawDevice device, Canvas c)
        {
            base.OnPaint(device, c);

            Vector2 center = device.TargetSize * 0.5f;

            int charOffset = 0;

#if __ANDROID__
            var fs = (DualityApp.SystemBackend.FileSystem as Duality.Backend.Android.NativeFileSystem);
            if (fs != null) {
                api.DrawMaterial(c, "MenuSettingsStorage", 180f, center.Y + 100f - 3f, Alignment.Right, ColorRgba.White);

                api.DrawStringShadow(device, ref charOffset, "Content Path:",
                    180f + 10f, center.Y + 100f, Alignment.Left, new ColorRgba(0.68f, 0.46f, 0.42f, 0.5f), 0.8f, charSpacing: 0.9f);

                api.DrawString(device, ref charOffset, fs.RootPath,
                    180f + 10f + 98f, center.Y + 100f, Alignment.Left, new ColorRgba(0.46f, 0.5f), 0.8f, charSpacing: 0.85f);
            }
#endif
        }

        private void Commit()
        {
            Resize = (ResizeMode)((ChoiceControl)controls[0]).SelectedIndex;

#if __ANDROID__
            Duality.Android.InnerView.allowVibrations = ((ChoiceControl)controls[1]).SelectedIndex == 1;

            Preferences.Set("Vibrations", Duality.Android.InnerView.allowVibrations);
#endif

            Preferences.Commit();
        }

        private void OnControlsPressed()
        {
            api.SwitchToSection(new ControlsSection());
        }
    }
}