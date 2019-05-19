using Duality.Backend;

namespace Jazz2.Storage
{
    public interface IPreferencesBackend : IDualityBackend
    {
        bool IsFirstRun { get; }

        T Get<T>(string key, T defaultValue);

        void Set<T>(string key, T value);

        void Remove(string key);

        void Commit();
    }
}