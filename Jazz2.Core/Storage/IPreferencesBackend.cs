using Duality.Backend;

namespace Jazz2.Storage
{
    public interface IPreferencesBackend : IDualityBackend
    {
        T Get<T>(string key, T defaultValue);

        void Set<T>(string key, T value);

        void Commit();
    }
}