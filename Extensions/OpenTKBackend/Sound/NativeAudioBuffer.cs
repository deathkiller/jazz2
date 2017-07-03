using System;
using Duality.Audio;
using OpenTK.Audio.OpenAL;

namespace Duality.Backend.DefaultOpenTK
{
	public class NativeAudioBuffer : INativeAudioBuffer
	{
		private int handle;
		public int Handle
		{
			get { return this.handle; }
		}

		public NativeAudioBuffer()
		{
			this.handle = AL.GenBuffer();
		}
		void INativeAudioBuffer.LoadData<T>(int sampleRate, T[] data, int dataLength, AudioDataLayout dataLayout, AudioDataElementType dataElementType)
		{
			ALFormat format = ALFormat.Mono16;
			if (dataLayout == AudioDataLayout.Mono)
			{
				if (dataElementType == AudioDataElementType.Byte)
					format = ALFormat.Mono8;
				else if (dataElementType == AudioDataElementType.Short)
					format = ALFormat.Mono16;
			}
			else if (dataLayout == AudioDataLayout.LeftRight)
			{
				if (dataElementType == AudioDataElementType.Byte)
					format = ALFormat.Stereo8;
				else if (dataElementType == AudioDataElementType.Short)
					format = ALFormat.Stereo16;
			}

			int sizeOfElement = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));

			AL.BufferData(
				this.handle,
				format,
				data,
				dataLength * sizeOfElement,
				sampleRate);

            //AudioBackend.CheckOpenALErrors();

        }
		void IDisposable.Dispose()
		{
			if (this.handle != 0)
			{
				AL.DeleteBuffer(this.handle);
				this.handle = 0;
			}
		}
	}
}