using Jazz2.Storage;

namespace Jazz2
{
    public class SettingsCache
    {
        public enum ResizeMode
        {
            None,
            HQ2x,
            xBRZ3,
            xBRZ4,
            CRT,
            GB
        }

#if PLATFORM_ANDROID || PLATFORM_WASM
        public static ResizeMode Resize = ResizeMode.None;
#else
        public static ResizeMode Resize = ResizeMode.xBRZ3;
#endif
        public static float MusicVolume = 0.7f;
        public static float SfxVolume = 0.85f;

        public static bool EnableLedgeClimb = true;
        public static bool EnableWeaponWheel = true;

        public static void Refresh()
        {
            Resize = (SettingsCache.ResizeMode)Preferences.Get<byte>("Resize", (byte)SettingsCache.Resize);
            MusicVolume = Preferences.Get<byte>("MusicVolume", (byte)(SettingsCache.MusicVolume * 100)) * 0.01f;
            SfxVolume = Preferences.Get<byte>("SfxVolume", (byte)(SettingsCache.SfxVolume * 100)) * 0.01f;

            EnableLedgeClimb = Preferences.Get<bool>("EnableLedgeClimb", true);
            EnableWeaponWheel = Preferences.Get<bool>("EnableWeaponWheel", true);
        }
    }
}