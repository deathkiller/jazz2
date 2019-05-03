using Duality;
using Jazz2.Storage;
using static Jazz2.SettingsCache;

namespace Jazz2.Game.UI.Menu.Settings
{
    public class SettingsSection : MenuSectionWithControls
    {
#if __ANDROID__
        private ChoiceControl vibrations;
        private SliderControl leftPadding;
        private SliderControl rightPadding;
#else
        private ChoiceControl screenMode;
        private ChoiceControl refreshMode;
#endif
        private SliderControl musicVolume;
        private SliderControl sfxVolume;

        public override void OnShow(IMenuContainer root)
        {
            base.OnShow(root);

#if !__ANDROID__
            ScreenMode screenModeCurrent = api.ScreenMode;
            int screenModeValue;
            if ((screenModeCurrent & ScreenMode.FullWindow) != 0) {
                screenModeValue = 1;
            } else {
                screenModeValue = 0;
            }
            screenMode = new ChoiceControl(api, "Screen Mode", screenModeValue, "Window", "Fullscreen");

            int refreshModeValue = (int)api.RefreshMode;
            refreshMode = new ChoiceControl(api, "Frame Rate Limit", refreshModeValue, "None (Not Recommended)", "60 fps", "V-Sync", "Adaptive V-Sync");
#endif
            musicVolume = new SliderControl(api, "Music Volume", MusicVolume, 0f, 1f);
            sfxVolume = new SliderControl(api, "SFX Volume", SfxVolume, 0f, 1f);

#if __ANDROID__
            vibrations = new ChoiceControl(api, "Vibrations", Android.InnerView.AllowVibrations ? 1 : 0, "Disable", "Enable");

            leftPadding = new SliderControl(api, "Left Controls Padding", Android.InnerView.LeftPadding, 0f, 0.15f);
            rightPadding = new SliderControl(api, "Right Controls Padding", Android.InnerView.RightPadding, 0f, 0.15f);

            controls = new MenuControlBase[] {
                new LinkControl(api, "Rescale Mode", OnRescaleModePressed),
                vibrations, musicVolume, sfxVolume,
                new LinkControl(api, "Controls", OnControlsPressed),
                leftPadding, rightPadding
            };
#else
            controls = new MenuControlBase[] {
                new LinkControl(api, "Rescale Mode", OnRescaleModePressed),
                screenMode, refreshMode, musicVolume, sfxVolume,
                new LinkControl(api, "Controls", OnControlsPressed)
            };
#endif
        }

        public override void OnHide(bool isRemoved)
        {
            if (isRemoved) {
                Commit();
            }

            base.OnHide(isRemoved);
        }

        private void Commit()
        {
            MusicVolume = musicVolume.CurrentValue;
            SfxVolume = sfxVolume.CurrentValue;

            Preferences.Set("MusicVolume", (byte)(MusicVolume * 100));
            Preferences.Set("SfxVolume", (byte)(SfxVolume * 100));

#if __ANDROID__
            Android.InnerView.AllowVibrations = (vibrations.SelectedIndex == 1);
            Preferences.Set("Vibrations", Android.InnerView.AllowVibrations);

            Android.InnerView.LeftPadding = leftPadding.CurrentValue;
            Preferences.Set("LeftPadding", (byte)(Android.InnerView.LeftPadding * 1000));

            Android.InnerView.RightPadding = rightPadding.CurrentValue;
            Preferences.Set("RightPadding", (byte)(Android.InnerView.RightPadding * 1000));
#else
            ScreenMode newScreenMode;
            switch (screenMode.SelectedIndex) {
                default:
                case 0: newScreenMode = ScreenMode.Window; break;
                case 1: newScreenMode = ScreenMode.FullWindow; break;
            }
            api.ScreenMode = newScreenMode;
            Preferences.Set("Screen", screenMode.SelectedIndex);

            api.RefreshMode = (RefreshMode)refreshMode.SelectedIndex;
            Preferences.Set("RefreshMode", refreshMode.SelectedIndex);
#endif

            Preferences.Commit();
        }

        private void OnRescaleModePressed()
        {
            api.SwitchToSection(new SettingsRescaleSection());
        }

        private void OnControlsPressed()
        {
            api.SwitchToSection(new ControlsSection());
        }
    }
}