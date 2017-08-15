using Duality;

namespace Jazz2.Storage
{
    public static class Preferences
    {
        private static IPreferencesBackend prefBack;

        public static T Get<T>(string key)
        {
            if (prefBack == null) DualityApp.InitBackend(out prefBack);

            return prefBack.Get<T>(key);
        }

        public static void Set<T>(string key, T value)
        {
            if (prefBack == null) DualityApp.InitBackend(out prefBack);

            prefBack.Set<T>(key, value);
        }

        public static void Commit()
        {
            if (prefBack == null) return;

            prefBack.Commit();
        }
    }
}