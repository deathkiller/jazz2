using System.IO;
using Duality.Audio;
using Duality.Backend;

namespace Duality.Resources
{
	/// <summary>
	/// Stores compressed audio data (Ogg Vorbis) in system memory as well as a reference to the
	/// OpenAL buffer containing actual PCM data, once set up. The OpenAL buffer is set up lazy
	/// i.e. as soon as demanded by accessing the AlBuffer property or calling SetupAlBuffer.
	/// </summary>
	/// <seealso cref="Duality.Resources.Sound"/>
	public class AudioData : Resource
	{
		private byte[] data = null;
		private bool forceStream = false;
		private INativeAudioBuffer native = null;

		/// <summary>
		/// [GET] The backends native audio buffer representation. Don't use this unless you know exactly what you're doing.
		/// </summary>
		public INativeAudioBuffer Native
		{
			get
			{
				if (this.native == null) this.SetupNativeBuffer();
				return this.native;
			}
		}
		///// <summary>
		///// [GET / SET] A data chunk representing raw audio data
		///// audio data.
		///// </summary>
		//public byte[] Data
		//{
		//	get { return this.data; }
		//	set
		//	{
		//		if (this.data != value) {
		//			this.data = value;
		//			this.DisposeNativeBuffer();
		//			this.SetupNativeBuffer();
		//		}
		//	}
		//}
		/// <summary>
		/// [GET / SET] If set to true, when playing a <see cref="Duality.Resources.Sound"/> that refers to this
		/// AudioData, it is forced to be played streamed. Normally, streaming kicks in automatically when playing
		/// very large sound files, such as music or large environmental ambience.
		/// </summary>
		public bool ForceStream
		{
			get { return this.forceStream; }
			set { this.forceStream = value; this.DisposeNativeBuffer(); }
		}
		/// <summary>
		/// [GET] Returns whether this AudioData will be played streamed.
		/// </summary>
		public bool IsStreamed
		{
			get { return this.forceStream || (this.data != null && this.data.Length > 1024 * 100); }
		}

		/// <summary>
		/// Creates a new, empty AudioData without any data.
		/// </summary>
		public AudioData() { }

		/// <summary>
		/// Creates a new AudioData based on an audio memory chunk.
		/// </summary>
		/// <param name="data">An audio memory chunk</param>
		public AudioData(byte[] data)
		{
			this.data = data;
			this.SetupNativeBuffer();
		}

		/// <summary>
		/// Creates a new AudioData based on a <see cref="System.IO.Stream"/> containing audio data.
		/// </summary>
		/// <param name="stream">A <see cref="System.IO.Stream"/> containing audio data</param>
		public AudioData(Stream stream)
		{
			if (stream.CanSeek) {
				this.data = new byte[stream.Length];
				stream.Read(this.data, 0, (int)stream.Length);
			} else {
				using (MemoryStream ms = new MemoryStream()) {
					stream.CopyTo(ms);
					this.data = ms.ToArray();
				}
			}

			this.SetupNativeBuffer();
		}

		/// <summary>
		/// Disposes the AudioDatas native buffer.
		/// </summary>
		/// <seealso cref="SetupNativeBuffer"/>
		private void DisposeNativeBuffer()
		{
			if (this.native != null) {
				this.native.Dispose();
				this.native = null;
			}
		}

		/// <summary>
		/// Sets up a new native buffer for this AudioData. This will result in decompressing
		/// the audio data and uploading it to OpenAL, unless the AudioData is streamed.
		/// </summary>
		private void SetupNativeBuffer()
		{
			// No AudioData available
			if (this.data == null || this.data.Length == 0) {
				this.DisposeNativeBuffer();
				return;
			}

			// Streamed Audio
			if (this.IsStreamed) {
				this.DisposeNativeBuffer();
				this.native = null;
			}
			// Non-Streamed Audio
			else {
				if (this.native == null) {
					this.native = DualityApp.AudioBackend.CreateBuffer();

					//PcmData pcm = OggVorbis.LoadFromMemory(this.data);
					//this.native.LoadData(
					//	pcm.SampleRate,
					//	pcm.Data,
					//	pcm.DataLength,
					//	pcm.ChannelCount == 1 ? AudioDataLayout.Mono : AudioDataLayout.LeftRight,
					//	AudioDataElementType.Short);

					byte[] samples;
					int sampleRate;
					AudioDataLayout layout;
					AudioDataElementType type;
					WavStreamHandle.Parse(this.data, out samples, out sampleRate, out layout, out type);

					int dataLength = (type == AudioDataElementType.Short ? samples.Length / 2 : samples.Length);

					this.native.LoadData(
						sampleRate,
						samples,
						dataLength,
						layout,
						type);

				} else {
					// Buffer already there? Do nothing.
				}
			}
		}

		protected override void OnDisposing(bool manually)
		{
			base.OnDisposing(manually);

			// Dispose unmanages Resources
			if (DualityApp.ExecContext != DualityApp.ExecutionContext.Terminated)
				this.DisposeNativeBuffer();

			// Get rid of the big data blob, so the GC can collect it.
			this.data = null;
		}
	}
}