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

        T IPreferencesBackend.Get<T>(string key)
        {
            return default(T);
        }

        void IPreferencesBackend.Set<T>(string key, T value)
        {
        }

        void IPreferencesBackend.Commit()
        {
        }
    }
}