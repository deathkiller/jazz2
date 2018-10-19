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
            CRT
        }

#if __ANDROID__
        public static ResizeMode Resize = ResizeMode.HQ2x;
        public static float MusicVolume = 0.7f;
        public static float SfxVolume = 0.3f;
#else
        public static ResizeMode Resize = ResizeMode.xBRZ3;
        public static float MusicVolume = 0.5f;
        public static float SfxVolume = 0.36f;
#endif
    }
}