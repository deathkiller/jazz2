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
            int targetLength = (int)Math.Ceiling(dataLength / pitch);

            // ToDo: Resampler skips samples with too high input sample rate

            GCHandle gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try {
                if (dataLayout == AudioDataLayout.Mono) {
                    // Duplicate mono to stereo
                    ResizeBuffer(targetLength * 2);

                    if (dataElementType == AudioDataElementType.Byte) {
                        // Duplicate channel, resample and convert to 16-bit
                        byte* ptr = (byte*)gcHandle.AddrOfPinnedObject();
                        for (int i = 0; i < targetLength; i++) {
                            float io = (float)i / targetLength * dataLength;

                            short sample1 = (short)((ptr[(int)io] - 0x80) << 8);
                            short sample2 = (short)((ptr[Math.Min((int)io + 1, dataLength - 1)] - 0x80) << 8);
                            short sample = (short)(sample1 + (sample2 - sample1) * (io % 1f));
                            InternalBuffer[Length++] = sample;
                            InternalBuffer[Length++] = sample;
                        }
                    } else if (dataElementType == AudioDataElementType.Short) {
                        // Duplicate channel and resample
                        short* ptr = (short*)gcHandle.AddrOfPinnedObject();
                        for (int i = 0; i < targetLength; i++) {
                            float io = (float)i / targetLength * dataLength;

                            short sample1 = ptr[(int)io];
                            short sample2 = ptr[Math.Min((int)io + 1, dataLength - 1)];
                            short sample = (short)(sample1 + (sample2 - sample1) * (io % 1f));
                            InternalBuffer[Length++] = sample;
                            InternalBuffer[Length++] = sample;
                        }
                    }
                } else if (dataLayout == AudioDataLayout.LeftRight) {
                    ResizeBuffer(targetLength);

                    if (dataElementType == AudioDataElementType.Byte) {
                        byte* ptr = (byte*)gcHandle.AddrOfPinnedObject();
                        if (targetSampleRate == sampleRate) {
                            // Resampling is not needed, convert 8-bit samples to 16-bit
                            for (int i = 0; i < dataLength; i++) {
                                short sample = (short)((ptr[i] - 0x80) << 8);
                                InternalBuffer[Length++] = sample;
                            }
                        } else {
                            // Resample and convert samples to 16-bit
                            targetLength /= 2;
                            int dataLengthPerChannel = dataLength / 2;

                            for (int i = 0; i < targetLength; i++) {
                                float io = (float)i / targetLength * dataLengthPerChannel;

                                short sample1 = (short)((ptr[(int)io] - 0x80) << 8);
                                short sample2 = (short)((ptr[Math.Min((int)io + 2, dataLength - 2)] - 0x80) << 8);
                                short sample = (short)(sample1 + (sample2 - sample1) * (io % 1f));
                                InternalBuffer[Length++] = sample;

                                sample1 = (short)((ptr[(int)io + 1] - 0x80) << 8);
                                sample2 = (short)((ptr[Math.Min((int)io + 3, dataLength - 1)] - 0x80) << 8);
                                sample = (short)(sample1 + (sample2 - sample1) * (io % 1f));
                                InternalBuffer[Length++] = sample;
                            }
                        }
                    } else if (dataElementType == AudioDataElementType.Short) {
                        short* ptr = (short*)gcHandle.AddrOfPinnedObject();
                        if (targetSampleRate == sampleRate) {
                            // Resampling is not needed
                            for (int i = 0; i < dataLength; i++) {
                                InternalBuffer[Length++] = ptr[i];
                            }
                        } else {
                            // Resample buffer to target sample rate
                            targetLength /= 2;
                            int dataLengthPerChannel = dataLength / 2;

                            for (int i = 0; i < targetLength; i++) {
                                float io = (float)i / targetLength * dataLengthPerChannel;

                                short sample1 = ptr[(int)io];
                                short sample2 = ptr[Math.Min((int)io + 2, dataLength - 2)];
                                short sample = (short)(sample1 + (sample2 - sample1) * (io % 1f));
                                InternalBuffer[Length++] = sample;

                                sample1 = ptr[(int)io + 1];
                                sample2 = ptr[Math.Min((int)io + 3, dataLength - 1)];
                                sample = (short)(sample1 + (sample2 - sample1) * (io % 1f));
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