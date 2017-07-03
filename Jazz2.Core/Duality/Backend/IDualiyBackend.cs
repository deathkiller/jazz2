namespace Duality.Backend
{
    public interface IDualityBackend
	{
		string Id { get; }
		string Name { get; }
		int Priority { get; }

		bool CheckAvailable();
		void Init();
		void Shutdown();
	}
}