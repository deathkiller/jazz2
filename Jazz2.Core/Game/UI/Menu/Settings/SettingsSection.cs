using System.Globalization;
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
        private ChoiceControl language;
        private string[] availableLanguages;

        private SliderControl musicVolume;
        private SliderControl sfxVolume;

        public override void OnShow(IMenuContainer root)
        {
            base.OnShow(root);

#if !__ANDROID__ && !WASM
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

#if !WASM
            musicVolume = new SliderControl(api, "menu/settings/music".T(), MusicVolume, 0f, 1f);
            sfxVolume = new SliderControl(api, "menu/settings/sfx".T(), SfxVolume, 0f, 1f);
#endif

#if __ANDROID__
            vibrations = new ChoiceControl(api, "menu/settings/vibrations".T(), Android.InnerView.AllowVibrations ? 1 : 0, "disabled".T(), "enabled".T());

            leftPadding = new SliderControl(api, "menu/settings/left padding".T(), Android.InnerView.LeftPadding, 0f, 0.15f);
            rightPadding = new SliderControl(api, "menu/settings/right padding".T(), Android.InnerView.RightPadding, 0f, 0.15f);

            controls = new MenuControlBase[] {
                new LinkControl(api, "menu/settings/rescale".T(), OnRescaleModePressed),
                language, vibrations, musicVolume, sfxVolume,
                new LinkControl(api, "menu/settings/controls".T(), OnControlsPressed),
                leftPadding, rightPadding
            };
#elif WASM
            controls = new MenuControlBase[] {
                new LinkControl(api, "menu/settings/rescale".T(), OnRescaleModePressed),
                language,
                new LinkControl(api, "menu/settings/controls".T(), OnControlsPressed)
            };
#else
            controls = new MenuControlBase[] {
                new LinkControl(api, "menu/settings/rescale".T(), OnRescaleModePressed),
                screenMode, refreshMode, language, musicVolume, sfxVolume,
                new LinkControl(api, "menu/settings/controls".T(), OnControlsPressed)
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
            string currentLanguage = availableLanguages[language.SelectedIndex];
            i18n.Language = currentLanguage;
            Preferences.Set("Language", currentLanguage);

#if !WASM
            MusicVolume = musicVolume.CurrentValue;
            SfxVolume = sfxVolume.CurrentValue;

            Preferences.Set("MusicVolume", (byte)(MusicVolume * 100));
            Preferences.Set("SfxVolume", (byte)(SfxVolume * 100));
#endif

#if __ANDROID__
            Android.InnerView.AllowVibrations = (vibrations.SelectedIndex == 1);
            Preferences.Set("Vibrations", Android.InnerView.AllowVibrations);

            Android.InnerView.LeftPadding = leftPadding.CurrentValue;
            Preferences.Set("LeftPadding", (byte)(Android.InnerView.LeftPadding * 1000));

            Android.InnerView.RightPadding = rightPadding.CurrentValue;
            Preferences.Set("RightPadding", (byte)(Android.InnerView.RightPadding * 1000));
#elif !WASM
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
            api.SwitchToSection(new RescaleSection());
        }

        private void OnControlsPressed()
        {
            api.SwitchToSection(new ControlsSection());
        }
    }
}