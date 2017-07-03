using System;
using System.IO;
using System.Text;

namespace Duality.Audio
{
    public static class WavStreamHandle
    {
        //private byte[] buffer;
        //private int sampleRate;
        //private AudioDataLayout layout;
        //private AudioDataElementType type;

        public static void Parse(byte[] audioData, out byte[] buffer, out int sampleRate, out AudioDataLayout layout, out AudioDataElementType type)
        {
            MemoryStream s = new MemoryStream(audioData, true);

            using (BinaryReader r = new BinaryReader(s, Encoding.ASCII, true)) {
                uint riff = r.ReadUInt32();
                if (riff != 0x46464952) {
                    throw new NotSupportedException();
                }
                uint riffChunkSize = r.ReadUInt32();

                uint wave = r.ReadUInt32();
                if (wave != 0x45564157) {
                    throw new NotSupportedException();
                }

                uint fmt = r.ReadUInt32();
                if (fmt != 0x20746d66) {
                    throw new NotSupportedException();
                }
                uint fmtChunkSize = r.ReadUInt32();

                ushort format = r.ReadUInt16();
                if (format != 0x1/*CODEC_PCM*/) {
                    throw new NotSupportedException();
                }
                ushort channels = r.ReadUInt16();
                sampleRate = r.ReadInt32();
                uint byteRate = r.ReadUInt32();

                ushort blockAlign = r.ReadUInt16();
                ushort bits = r.ReadUInt16();

                if (channels == 2) {
                    layout = AudioDataLayout.LeftRight;;
                } else if (channels == 1) {
                    layout = AudioDataLayout.Mono;
                } else {
                    throw new NotSupportedException("Unsupported channels " + channels);
                }

                if (bits == 8) {
                    type = AudioDataElementType.Byte;
                } else if (bits == 16) {
                    type = AudioDataElementType.Short;
                } else {
                    throw new NotSupportedException("Unsupported bits " + bits);
                }

                uint data = r.ReadUInt32();
                if (data != 0x61746164) {
                    throw new NotSupportedException();
                }
                int dataChunkSize = r.ReadInt32();

                buffer = r.ReadBytes(dataChunkSize);
            }
        }

        //public void StreamChunk(out byte[] buffer, out int sampleRate, out AudioDataLayout layout, out AudioDataElementType type)
        //{
        //    buffer = this.buffer;
        //    sampleRate = this.sampleRate;
        //    layout = this.layout;
        //    type = this.type;
        //
        //    //pcm.DataLength = 0;
        //    //pcm.ChannelCount = handle.VorbisInstance.Channels;
        //    //pcm.SampleRate = handle.VorbisInstance.SampleRate;
        //
        //    /*bool eof = false;
        //    float[] buffer = new float[bufferSize];
        //    while (pcm.DataLength < buffer.Length) {
        //        int samplesRead;
        //        lock (readMutex) {
        //            samplesRead = handle.VorbisInstance.ReadSamples(buffer, pcm.DataLength, buffer.Length - pcm.DataLength);
        //        }
        //        if (samplesRead > 0) {
        //            pcm.DataLength += samplesRead;
        //        } else {
        //            eof = true;
        //            break;
        //        }
        //    }
        //
        //    pcm.Data = new short[pcm.DataLength];
        //    CastBuffer(buffer, pcm.Data, 0, pcm.DataLength);
        //
        //    return pcm.DataLength > 0 && !eof;*/
        //}
    }
}