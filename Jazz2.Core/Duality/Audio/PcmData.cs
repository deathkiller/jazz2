namespace Duality.Audio
{
    public struct PcmData
	{
		public const int SizeOfDataElement = sizeof(short);

		public	short[]	Data;
		public	int		DataLength;
		public	int		ChannelCount;
		public	int		SampleRate;
	}
}