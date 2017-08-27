using System.Diagnostics;
using System.Threading;
using Android.Media;

namespace Duality.Backend.Android
{
    public class AudioBackend : IAudioBackend
    {
        private static AudioBackend activeInstance;
        public static AudioBackend ActiveInstance
        {
            get { return activeInstance; }
        }

        private const int DefaultSampleRate = 44100;
        private const ChannelOut DefaultChannels = ChannelOut.Stereo;
        private const Encoding DefaultEncoding = Encoding.Pcm16bit;

        private AudioTrack masterTrack;
        private int bufferSizeSamples;
        private int samplesWritten;

        private Thread streamWorker;
        private RawList<NativeAudioSource> streamWorkerQueue;
        private AutoResetEvent streamWorkerQueueEvent;
        private bool streamWorkerEnd;

        internal Vector3 ListenerPosition;
        private bool mute;

        int IAudioBackend.AvailableSources
        {
            get { return 32; }
        }
        int IAudioBackend.MaxSourceCount
        {
            get { return 32; }
        }
        string IDualityBackend.Id
        {
            get { return "AndroidAudioBackend"; }
        }
        string IDualityBackend.Name
        {
            get { return "Android AudioTrack Backend"; }
        }
        int IDualityBackend.Priority
        {
            get { return 0; }
        }

        bool IDualityBackend.CheckAvailable()
        {
            return true;
        }

        void IDualityBackend.Init()
        {
            activeInstance = this;

            int bufferSize = AudioTrack.GetMinBufferSize(DefaultSampleRate, DefaultChannels, DefaultEncoding);
            masterTrack = new AudioTrack(Stream.Music, DefaultSampleRate, DefaultChannels, DefaultEncoding, bufferSize, AudioTrackMode.Stream);
            masterTrack.Play();

            bufferSizeSamples = bufferSize * sizeof(ushort);

            // Set up the streaming thread
            streamWorkerEnd = false;
            streamWorkerQueue = new RawList<NativeAudioSource>();
            streamWorkerQueueEvent = new AutoResetEvent(false);
            streamWorker = new Thread(ThreadStreamFunc);
            streamWorker.IsBackground = true;
            streamWorker.Start();
        }

        void IDualityBackend.Shutdown()
        {
            // Shut down the streaming thread
            if (streamWorker != null) {
                streamWorkerEnd = true;
                if (!streamWorker.Join(1000)) {
                    streamWorker.Abort();
                }
                streamWorkerQueueEvent.Dispose();
                streamWorkerEnd = false;
                streamWorkerQueueEvent = null;
                streamWorkerQueue = null;
                streamWorker = null;
            }

            if (activeInstance == this) {
                activeInstance = null;
            }
        }

        void IAudioBackend.UpdateWorldSettings(float speedOfSound, float dopplerFactor)
        {
            // ToDo: Implement world settigs
        }
        void IAudioBackend.UpdateListener(Vector3 position, Vector3 velocity, float angle, bool mute)
        {
            // ToDo: Implement velocity
            // ToDo: Implement angle

            this.ListenerPosition = position;
            this.mute = mute;
        }

        INativeAudioBuffer IAudioBackend.CreateBuffer()
        {
            return new NativeAudioBuffer(DefaultSampleRate);
        }
        INativeAudioSource IAudioBackend.CreateSource()
        {
            return new NativeAudioSource();
        }

        internal void EnqueueForStreaming(NativeAudioSource source)
        {
            if (streamWorkerQueue.Contains(source))
                return;

            streamWorkerQueue.Add(source);
            streamWorkerQueueEvent.Set();
        }

        private void ThreadStreamFunc()
        {
            short[] buffer = new short[bufferSizeSamples];

            Stopwatch watch = new Stopwatch();
            watch.Restart();

            while (!streamWorkerEnd) {
                // Process even number of samples
                int samplesNeeded = ((CalculateSamplesNeeded() * 2) / 12) & ~1;

                for (int j = 0; j < streamWorkerQueue.Count; j++) {
                    NativeAudioSource source = streamWorkerQueue[j];

                    // Transfer samples to buffer
                    int bufferPos = 0;
                    while (bufferPos < samplesNeeded && source.QueuedBuffers.Count > 0) {
                        int bufferIndex = source.QueuedBuffers.Peek();
                        ref int playbackPos = ref source.QueuedBuffersPos[bufferIndex];

                        NativeAudioBuffer sourceBuffer = source.AvailableBuffers[bufferIndex];
                        int samplesInBuffer = MathF.Min(sourceBuffer.Length - playbackPos, samplesNeeded - bufferPos);
                        //int samplesInBuffer = MathF.Min((int)((sourceBuffer.Length - playbackPos) / source.LastState.Pitch), samplesNeeded - bufferPos);
                        if (!mute) {
                            //if (MathF.Abs(1f - source.LastState.Pitch) < 0.01f) {
                            for (int i = 0; i < samplesInBuffer; i += 2) {
                                short sampleLeft = (short)(sourceBuffer.InternalBuffer[playbackPos + i] * source.VolumeLeft);
                                short sampleRight = (short)(sourceBuffer.InternalBuffer[playbackPos + i + 1] * source.VolumeRight);

                                // Fast check to prevent clipping
                                // ToDo: Do this better somehow...
                                if (MathF.Abs(buffer[bufferPos] + sampleLeft) < short.MaxValue &&
                                    MathF.Abs(buffer[bufferPos + 1] + sampleRight) < short.MaxValue) {
                                    buffer[bufferPos] += sampleLeft;
                                    buffer[bufferPos + 1] += sampleRight;
                                }

                                bufferPos += 2;
                            }
                            //} else {
                            //    // ToDo: Check this pitch changing...
                            //    for (int i = 0; i < samplesInBuffer; i += 2) {

                            //        float io = playbackPos + (int)(i * source.LastState.Pitch);

                            //        short sample11 = sourceBuffer.InternalBuffer[(int)io];
                            //        short sample12 = sourceBuffer.InternalBuffer[MathF.Min((int)io + 2, sourceBuffer.Length - 2)];

                            //        short sampleLeft = (short)((sample11 + (sample12 - sample11) * (io % 1f)) * source.VolumeLeft);

                            //        short sample21 = sourceBuffer.InternalBuffer[MathF.Min((int)io + 1, sourceBuffer.Length - 1)];
                            //        short sample22 = sourceBuffer.InternalBuffer[MathF.Min((int)io + 3, sourceBuffer.Length - 1)];
                            //        short sampleRight = (short)((sample21 + (sample22 - sample21) * (io % 1f)) * source.VolumeRight);

                            //        // Fast check to prevent clipping
                            //        // ToDo: Do this better somehow...
                            //        if (MathF.Abs(buffer[bufferPos] + sampleLeft) < short.MaxValue &&
                            //            MathF.Abs(buffer[bufferPos + 1] + sampleRight) < short.MaxValue) {
                            //            buffer[bufferPos] += sampleLeft;
                            //            buffer[bufferPos + 1] += sampleRight;
                            //        }

                            //        bufferPos += 2;
                            //    }
                            //}
                        }
                        playbackPos += samplesInBuffer;
                        //playbackPos += (int)(samplesInBuffer * source.LastState.Pitch);

                        if (playbackPos >= sourceBuffer.Length) {
                            playbackPos = NativeAudioSource.UnqueuedBuffer;
                            source.QueuedBuffers.Dequeue();
                        }
                    }

                    // Perform the necessary streaming operations on the audio source, and remove it when requested
                    if (source.IsStreamed) {
                        // Try to stream new data
                        if (source.IsStopped || !source.PerformStreaming()) {
                            // End of stream, remove from queue
                            streamWorkerQueue.RemoveAtFast(j);
                            j--;
                        }
                    } else {
                        if (source.QueuedBuffers.Count == 0) {
                            if (source.LastState.Looped) {
                                // Enqueue sample again
                                source.QueuedBuffers.Enqueue(0);
                                source.QueuedBuffersPos[0] = 0;
                            } else {
                                // End of sample, remove from queue
                                streamWorkerQueue.RemoveAtFast(j);
                                j--;
                            }
                        }
                    }
                }

                // Write buffer to Master Audio Track
                masterTrack.Write(buffer, 0, samplesNeeded);
                samplesWritten += samplesNeeded >> 1;

                // Erase buffer for next batch
                for (int i = 0; i < samplesNeeded; i++) {
                    buffer[i] = 0;
                }

                // After each roundtrip, sleep a little, don't keep the processor busy for no reason
                watch.Stop();
                int roundtripTime = (int)watch.ElapsedMilliseconds;
                if (roundtripTime <= 1) {
                    streamWorkerQueueEvent.WaitOne(16);
                }
                watch.Restart();
            }
        }

        private int CalculateSamplesNeeded()
        {
            int playing = masterTrack.PlaybackHeadPosition;
            int maxSamples = (bufferSizeSamples / 2); // / Channel count
            return (playing + maxSamples - samplesWritten);
        }
    }
}