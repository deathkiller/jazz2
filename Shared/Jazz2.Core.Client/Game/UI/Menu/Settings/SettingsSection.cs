using System.Globalization;
using Duality;
using Jazz2.Storage;
using static Jazz2.SettingsCache;

namespace Jazz2.Game.UI.Menu.Settings
{
    public class SettingsSection : MenuSectionWithControls
    {
#if PLATFORM_ANDROID
        private ChoiceControl vibrations;
        private SliderControl controlsOpacity;
        private SliderControl leftPadding;
        private SliderControl rightPadding;
        private SliderControl bottomPadding1;
        private SliderControl bottomPadding2;
#elif !PLATFORM_WASM
        private ChoiceControl screenMode;
        private ChoiceControl refreshMode;
#endif
        private ChoiceControl language;
        private string[] availableLanguages;

        private SliderControl musicVolume;
        private SliderControl sfxVolume;

        private ChoiceControl enableLedgeClimb;

        public override void OnShow(IMenuContainer root)
        {
            base.OnShow(root);

#if !PLATFORM_ANDROID && !PLATFORM_WASM
            ScreenMode screenModeCurrent = api.ScreenMode;
            int screenModeValue;
            if ((screenModeCurrent & ScreenMode.FullWindow) != 0) {
                screenModeValue = 1;
            } else {
                screenModeValue = 0;
            }
            screenMode = new ChoiceControl(api, "menu/settings/screen".T(), screenModeValue,
                "menu/settings/screen/0".T(), "menu/settings/screen/1".T());

            int refreshModeValue = (int)api.RefreshMode;
            refreshMode = new ChoiceControl(api, "menu/settings/refresh".T(), refreshModeValue,
                "menu/settings/refresh/0".T(), "menu/settings/refresh/1".T(), "menu/settings/refresh/2".T(), "menu/settings/refresh/3".T());
#endif
            availableLanguages = i18n.AvailableLanguages;
            string currentLanguage = i18n.Language;
            int currentLanguageIndex = 0;
            string[] languageNames = new string[availableLanguages.Length];
            for (int i = 0; i < availableLanguages.Length; i++) {
                if (availableLanguages[i] == currentLanguage) {
                    currentLanguageIndex = i;
                }

                try {
                    languageNames[i] = CultureInfo.GetCultureInfo(availableLanguages[i]).DisplayName;
                } catch {
                    languageNames[i] = availableLanguages[i].ToUpperInvariant();
                }
            }
            language = new ChoiceControl(api, "menu/settings/language".T(), currentLanguageIndex, languageNames);

#if !PLATFORM_WASM
            musicVolume = new SliderControl(api, "menu/settings/music".T(), MusicVolume, 0f, 1f);
            sfxVolume = new SliderControl(api, "menu/settings/sfx".T(), SfxVolume, 0f, 1f);
#endif
            enableLedgeClimb = new ChoiceControl(api, "menu/settings/ledge climb".T(), EnableLedgeClimb ? 1 : 0, "disabled".T(), "enabled".T());

#if PLATFORM_ANDROID
            vibrations = new ChoiceControl(api, "menu/settings/vibrations".T(), Android.InnerView.AllowVibrations ? 1 : 0, "disabled".T(), "enabled".T());

            controlsOpacity = new SliderControl(api, "menu/settings/controls opacity".T(), Android.InnerView.ControlsOpacity, 0f, 1f);
            leftPadding = new SliderControl(api, "menu/settings/left padding".T(), Android.InnerView.LeftPadding, 0f, 0.15f);
            rightPadding = new SliderControl(api, "menu/settings/right padding".T(), Android.InnerView.RightPadding, 0f, 0.15f);
            bottomPadding1 = new SliderControl(api, "menu/settings/bottom padding 1".T(), Android.InnerView.BottomPadding1, -0.15f, 0.15f);
            bottomPadding2 = new SliderControl(api, "menu/settings/bottom padding 2".T(), Android.InnerView.BottomPadding2, -0.15f, 0.15f);

            controls = new MenuControlBase[] {
                new LinkControl(api, "menu/settings/rescale".T(), OnRescaleModePressed),
                language, vibrations, musicVolume, sfxVolume, enableLedgeClimb,
                new LinkControl(api, "menu/settings/controls".T(), OnControlsPressed),
                controlsOpacity, leftPadding, rightPadding, bottomPadding1, bottomPadding2
            };
#elif PLATFORM_WASM
            controls = new MenuControlBase[] {
                new LinkControl(api, "menu/settings/rescale".T(), OnRescaleModePressed),
                language, enableLedgeClimb,
                new LinkControl(api, "menu/settings/controls".T(), OnControlsPressed)
            };
#else
            controls = new MenuControlBase[] {
                new LinkControl(api, "menu/settings/rescale".T(), OnRescaleModePressed),
                screenMode, refreshMode, language, musicVolume, sfxVolume, enableLedgeClimb,
                new LinkControl(api, "menu/settings/controls".T(), OnControlsPressed)
            };
#endif
        }

        public override void OnHide(bool isRemoved)
        {
            Commit();

            base.OnHide(isRemoved);
        }

        private void Commit()
        {
            string currentLanguage = availableLanguages[language.SelectedIndex];
            bool languageChanged = (currentLanguage != i18n.Language);
            i18n.Language = currentLanguage;
            Preferences.Set("Language", currentLanguage);

#if !PLATFORM_WASM
            MusicVolume = musicVolume.CurrentValue;
            SfxVolume = sfxVolume.CurrentValue;

            Preferences.Set("MusicVolume", (byte)(MusicVolume * 100));
            Preferences.Set("SfxVolume", (byte)(SfxVolume * 100));
#endif
            EnableLedgeClimb = (enableLedgeClimb.SelectedIndex == 1);
            Preferences.Set("EnableLedgeClimb", EnableLedgeClimb);

#if PLATFORM_ANDROID
            Android.InnerView.AllowVibrations = (vibrations.SelectedIndex == 1);
            Preferences.Set("Vibrations", Android.InnerView.AllowVibrations);

            Android.InnerView.ControlsOpacity = controlsOpacity.CurrentValue;
            Preferences.Set("ControlsOpacity", (byte)(Android.InnerView.ControlsOpacity * 255));

            Android.InnerView.LeftPadding = leftPadding.CurrentValue;
            Preferences.Set("LeftPadding", (byte)(Android.InnerView.LeftPadding * 1000));

            Android.InnerView.RightPadding = rightPadding.CurrentValue;
            Preferences.Set("RightPadding", (byte)(Android.InnerView.RightPadding * 1000));

            Android.InnerView.BottomPadding1 = bottomPadding1.CurrentValue;
            Preferences.Set("BottomPadding1", (byte)((Android.InnerView.BottomPadding1 * 500) + 128));

            Android.InnerView.BottomPadding2 = bottomPadding2.CurrentValue;
            Preferences.Set("BottomPadding2", (byte)((Android.InnerView.BottomPadding2 * 500) + 128));
#elif !PLATFORM_WASM
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

            if (languageChanged) {
                api.Recreate();
            }
        }

        private void OnRescaleModePressed()
        {
            api.SwitchToSection(new RescaleSection());
        }

        private void OnControlsPressed()
        {
            api.SwitchToSection(new ControlsSection());
        }
    }
}