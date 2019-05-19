using System;
using Duality;

namespace Jazz2.Storage
{
    public static class Preferences
    {
        private static IPreferencesBackend prefBack;

        public static bool IsFirstRun
        {
            get
            {
                if (prefBack == null) {
                    Initialize();
                }

                return prefBack.IsFirstRun;
            }
        }

        public static T Get<T>(string key, T defaultValue = default(T))
        {
            if (prefBack == null) {
                Initialize();
            }

            return prefBack.Get<T>(key, defaultValue);
        }

        public static void Set<T>(string key, T value)
        {
            if (prefBack == null) {
                Initialize();
            }

            prefBack.Set<T>(key, value);
        }

        public static void Remove(string key)
        {
            if (prefBack == null) {
                Initialize();
            }

            prefBack.Remove(key);
        }

        public static void Commit()
        {
            if (prefBack == null) {
                Initialize();
            }

            prefBack.Commit();
        }

        private static void Initialize()
        {
            DualityApp.InitBackend(out prefBack);

            DualityApp.Terminating += OnDualityAppTerminating;
        }

        private static void OnDualityAppTerminating(object sender, EventArgs e)
        {
            DualityApp.Terminating -= OnDualityAppTerminating;

            DualityApp.ShutdownBackend(ref prefBack);
        }
    }
}