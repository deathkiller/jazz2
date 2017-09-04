using System;
using System.Runtime.InteropServices;
using Duality.Audio;

namespace Duality.Backend.Android
{
    public class NativeAudioBuffer : INativeAudioBuffer
    {
        private readonly int targetSampleRate;

        internal short[] InternalBuffer;
        internal int Length;

        public NativeAudioBuffer(int targetSampleRate)
        {
            this.targetSampleRate = targetSampleRate;
        }

        unsafe void INativeAudioBuffer.LoadData<T>(int sampleRate, T[] data, int dataLength, AudioDataLayout dataLayout, AudioDataElementType dataElementType)
        {
            float pitch = (float)sampleRate / targetSampleRate;
            int targetLengthPerChannel = (int)Math.Ceiling(dataLength / pitch);

            // ToDo: Resampler skips samples with too high input sample rate (and too low output sample rate),
            // but that almost never happens on Android anyway

            GCHandle gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try {
                if (dataLayout == AudioDataLayout.Mono) {
                    // Duplicate mono input to stereo output
                    ResizeBuffer(targetLengthPerChannel * 2);

                    if (dataElementType == AudioDataElementType.Byte) {
                        // Duplicate channel, resample and convert 8-bit samples to 16-bit samples
                        byte* ptr = (byte*)gcHandle.AddrOfPinnedObject();
                        for (int destIndex = 0; destIndex < targetLengthPerChannel; destIndex++) {
                            float srcIndex = (float)destIndex / targetLengthPerChannel * dataLength;

                            short sample1 = (short)((ptr[(int)srcIndex] - 0x80) << 8);
                            short sample2 = (short)((ptr[Math.Min((int)srcIndex + 1, dataLength - 1)] - 0x80) << 8);
                            short sample = (short)(sample1 + (sample2 - sample1) * (srcIndex % 1f));
                            InternalBuffer[Length++] = sample;
                            InternalBuffer[Length++] = sample;
                        }
                    } else if (dataElementType == AudioDataElementType.Short) {
                        // Duplicate channel and resample
                        short* ptr = (short*)gcHandle.AddrOfPinnedObject();
                        for (int destIndex = 0; destIndex < targetLengthPerChannel; destIndex++) {
                            float srcIndex = (float)destIndex / targetLengthPerChannel * dataLength;

                            short sample1 = ptr[(int)srcIndex];
                            short sample2 = ptr[Math.Min((int)srcIndex + 1, dataLength - 1)];
                            short sample = (short)(sample1 + (sample2 - sample1) * (srcIndex % 1f));
                            InternalBuffer[Length++] = sample;
                            InternalBuffer[Length++] = sample;
                        }
                    }
                } else if (dataLayout == AudioDataLayout.LeftRight) {
                    // Channels are already interleaved, "targetLengthPerChannel" will
                    // be divided by channel count later, it's target length right here
                    ResizeBuffer(targetLengthPerChannel);

                    if (dataElementType == AudioDataElementType.Byte) {
                        byte* ptr = (byte*)gcHandle.AddrOfPinnedObject();
                        if (targetSampleRate == sampleRate) {
                            // Resampling is not needed, convert 8-bit samples to 16-bit samples
                            for (int srcIndex = 0; srcIndex < dataLength; srcIndex++) {
                                short sample = (short)((ptr[srcIndex] - 0x80) << 8);
                                InternalBuffer[Length++] = sample;
                            }
                        } else {
                            // Resample and convert samples to 16-bit samples
                            targetLengthPerChannel /= 2;
                            int dataLengthPerChannel = dataLength / 2;

                            for (int destIndex = 0; destIndex < targetLengthPerChannel; destIndex++) {
                                float srcIndex = (float)destIndex / targetLengthPerChannel * dataLengthPerChannel;

                                short sample1 = (short)((ptr[(int)srcIndex] - 0x80) << 8);
                                short sample2 = (short)((ptr[Math.Min((int)srcIndex + 2, dataLength - 2)] - 0x80) << 8);
                                short sample = (short)(sample1 + (sample2 - sample1) * (srcIndex % 1f));
                                InternalBuffer[Length++] = sample;

                                sample1 = (short)((ptr[(int)srcIndex + 1] - 0x80) << 8);
                                sample2 = (short)((ptr[Math.Min((int)srcIndex + 3, dataLength - 1)] - 0x80) << 8);
                                sample = (short)(sample1 + (sample2 - sample1) * (srcIndex % 1f));
                                InternalBuffer[Length++] = sample;
                            }
                        }
                    } else if (dataElementType == AudioDataElementType.Short) {
                        short* ptr = (short*)gcHandle.AddrOfPinnedObject();
                        if (targetSampleRate == sampleRate) {
                            // Resampling is not needed
                            for (int srcIndex = 0; srcIndex < dataLength; srcIndex++) {
                                InternalBuffer[Length++] = ptr[srcIndex];
                            }
                        } else {
                            // Resample buffer to target sample rate
                            targetLengthPerChannel /= 2;
                            int dataLengthPerChannel = dataLength / 2;

                            for (int destIndex = 0; destIndex < targetLengthPerChannel; destIndex++) {
                                float srcIndex = (float)destIndex / targetLengthPerChannel * dataLengthPerChannel;

                                short sample1 = ptr[(int)srcIndex];
                                short sample2 = ptr[Math.Min((int)srcIndex + 2, dataLength - 2)];
                                short sample = (short)(sample1 + (sample2 - sample1) * (srcIndex % 1f));
                                InternalBuffer[Length++] = sample;

                                sample1 = ptr[(int)srcIndex + 1];
                                sample2 = ptr[Math.Min((int)srcIndex + 3, dataLength - 1)];
                                sample = (short)(sample1 + (sample2 - sample1) * (srcIndex % 1f));
                                InternalBuffer[Length++] = sample;
                            }
                        }
                    }
                }
            } finally {
                gcHandle.Free();
            }
        }

        void IDisposable.Dispose()
        {
            // Only reset buffer size, buffers could be reused
            Length = 0;
        }

        /// <summary>
        /// Resize existing internal buffer or create new one
        /// </summary>
        /// <param name="targetLength">Target size in samples</param>
        private void ResizeBuffer(int targetLength)
        {
            if (InternalBuffer == null) {
                InternalBuffer = new short[targetLength];
            } else if (InternalBuffer.Length - Length < targetLength) {
                Array.Resize(ref InternalBuffer, targetLength);
            }
        }
    }
}