using Duality.Backend;

namespace Jazz2.Storage.Dummy
{
    public class DummyPreferencesBackend : IPreferencesBackend
    {
        string IDualityBackend.Id => "DummyPreferencesBackend";

        string IDualityBackend.Name => "No Preferences";

        int IDualityBackend.Priority => int.MinValue;

        bool IDualityBackend.CheckAvailable()
        {
            return true;
        }

        void IDualityBackend.Init() { }
        void IDualityBackend.Shutdown() { }

        T IPreferencesBackend.Get<T>(string key, T defaultValue)
        {
            return defaultValue;
        }

        void IPreferencesBackend.Set<T>(string key, T value)
        {
        }

        void IPreferencesBackend.Remove(string key)
        {
        }

        void IPreferencesBackend.Commit()
        {
        }
    }
}