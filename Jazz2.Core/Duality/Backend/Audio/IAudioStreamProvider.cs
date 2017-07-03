namespace Duality.Backend
{
    public interface IAudioStreamProvider
	{
		void OpenStream();
		bool ReadStream(INativeAudioBuffer targetBuffer);
		void CloseStream();
	}
}