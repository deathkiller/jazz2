using System;
using System.Collections.Generic;

namespace Duality.Backend.Android
{
    public class NativeAudioSource : INativeAudioSource
    {
        private enum StopRequest
        {
            None,
            EndOfStream,
            Immediately
        }

        public const int UnqueuedBuffer = -1;

        private bool isInitial = true;
        private bool isStreamStarted;
        private bool isFirstUpdate = true;

        private IAudioStreamProvider streamProvider;
        private StopRequest strStopReq = StopRequest.None;

        internal bool IsStreamed;
        internal bool IsStopped;
        internal AudioSourceState LastState = AudioSourceState.Default;

        internal NativeAudioBuffer[] AvailableBuffers;
        internal Queue<int> QueuedBuffers = new Queue<int>();
        internal int[] QueuedBuffersPos;

        internal float VolumeLeft, VolumeRight;

        bool INativeAudioSource.IsInitial
        {
            get { return this.isInitial; }
        }

        bool INativeAudioSource.IsFinished
        {
            get
            {
                if (this.isInitial)
                    return false;

                // Stopped and either not streamed or requesting to end.
                if (IsStopped &&
                    (!this.IsStreamed || this.strStopReq != StopRequest.None))
                    return true;
                // Not even started playing, but requested to end anyway.
                else if (IsStopped && this.strStopReq == StopRequest.Immediately)
                    return true;
                // Not finished yet.
                else
                    return false;
            }
        }

        void INativeAudioSource.Play(INativeAudioBuffer buffer)
        {
            if (!this.isInitial)
                throw new InvalidOperationException(
                    "Native audio source already in use. To re-use an audio source, reset it first.");
            this.isInitial = false;
            IsStopped = false;

            this.strStopReq = StopRequest.None;

            NativeAudioBuffer newBuffer = buffer as NativeAudioBuffer;
            AvailableBuffers = new[] {newBuffer};
            QueuedBuffersPos = new[] {0};
            QueuedBuffers.Enqueue(0);

            AudioBackend.ActiveInstance.EnqueueForStreaming(this);
        }

        void INativeAudioSource.Play(IAudioStreamProvider streamingProvider)
        {
            if (!isInitial)
                throw new InvalidOperationException(
                    "Native audio source already in use. To re-use an audio source, reset it first.");
            isInitial = false;
            IsStopped = false;

            IsStreamed = true;
            strStopReq = StopRequest.None;
            streamProvider = streamingProvider;
            isStreamStarted = false;

            AudioBackend.ActiveInstance.EnqueueForStreaming(this);
        }

        void INativeAudioSource.Stop()
        {
            strStopReq = StopRequest.Immediately;
        }

        void INativeAudioSource.Reset()
        {
            ResetLocalState();
            ResetSourceState();
            isInitial = true;
            isFirstUpdate = true;
        }

        void INativeAudioSource.ApplyState(ref AudioSourceState state)
        {
            // ToDo: Implement velocity
            // ToDo: Implement pitch
            // ToDo: Implement lowpass
            // ToDo: Implement pause

            if (isFirstUpdate ||
                LastState.RelativeToListener != state.RelativeToListener ||
                LastState.Position != state.Position ||
                LastState.MaxDistance != state.MaxDistance ||
                LastState.MinDistance != state.MinDistance ||
                LastState.Volume != state.Volume) {

                Vector3 distance;
                if (state.RelativeToListener) {
                    distance = state.Position;
                } else {
                    distance = state.Position - AudioBackend.ActiveInstance.ListenerPosition;
                }

                float maxDistance = (state.MaxDistance - state.MinDistance);
                float distanceRatio = (Math.Max(distance.Length - state.MinDistance, 0) / maxDistance);
                distanceRatio = state.Volume * (1f - distanceRatio * distanceRatio);

                float distanceSideRatio = MathF.Sign(distance.X) * Math.Max(Math.Abs(distance.X) - state.MinDistance * 0.5f, 0) / maxDistance;
                if (Math.Abs(distanceSideRatio) < 0.00001f) {
                    VolumeLeft = VolumeRight = 1f;
                } else {
                    const float stereoSeparation = 0.5f;

                    float ratio = MathF.Clamp(distanceSideRatio * 8f, -stereoSeparation, stereoSeparation);
                    VolumeLeft = stereoSeparation - ratio;
                    VolumeRight = stereoSeparation + ratio;
                }

                VolumeLeft *= distanceRatio;
                VolumeRight *= distanceRatio;
            }

            LastState = state;
            LastState.Looped = state.Looped && !IsStreamed;
            isFirstUpdate = false;
        }

        void IDisposable.Dispose()
        {
            if (IsStreamed) {
                streamProvider.CloseStream();
                strStopReq = StopRequest.Immediately;
            }

            this.ResetSourceState();
        }

        private void ResetLocalState()
        {
            strStopReq = StopRequest.Immediately;
            IsStreamed = false;
            streamProvider = null;
            IsStopped = false;
        }

        private void ResetSourceState()
        {
            QueuedBuffers.Clear();
        }

        internal bool PerformStreaming()
        {
            if (IsStopped && strStopReq != StopRequest.None) {
                // Stopped due to regular EOF. If strStopReq is NOT set,
                // the source stopped playing because it reached the end of the buffer
                // but in fact only because we were too slow inserting new data.
                return false;
            } else if (strStopReq == StopRequest.Immediately) {
                // Stopped intentionally due to Stop()
                IsStopped = true;
                return false;
            }

            if (!isStreamStarted) {
                isStreamStarted = true;

                // Initialize streaming
                PerformStreamingBegin();
            } else {
                // Stream new data
                PerformStreamingUpdate();
            }

            return true;
        }

        private void PerformStreamingBegin()
        {
            // Generate streaming buffers
            AvailableBuffers = new NativeAudioBuffer[3];
            QueuedBuffersPos = new int[3];
            for (int i = 0; i < AvailableBuffers.Length; i++) {
                AvailableBuffers[i] = DualityApp.AudioBackend.CreateBuffer() as NativeAudioBuffer;
            }

            // Begin streaming
            streamProvider.OpenStream();

            // Initially, completely fill all buffers
            for (int i = 0; i < AvailableBuffers.Length; i++) {
                bool eof = !streamProvider.ReadStream(AvailableBuffers[i]);
                if (!eof) {
                    QueuedBuffers.Enqueue(i);
                } else {
                    break;
                }
            }
        }

        private void PerformStreamingUpdate()
        {
            while (true) {
                int unqueuedBufferIndex = UnqueuedBuffer;
                for (int i = 0; i < AvailableBuffers.Length; i++) {
                    NativeAudioBuffer buffer = AvailableBuffers[i];
                    if (QueuedBuffersPos[i] == UnqueuedBuffer) {
                        // Buffer is played to the end - rewind it to the start and use it...
                        buffer.Length = 0;
                        QueuedBuffersPos[i] = 0;
                        unqueuedBufferIndex = i;
                        break;
                    }
                }

                if (unqueuedBufferIndex == UnqueuedBuffer) {
                    break;
                }

                // Stream data to the buffer
                bool eof = !streamProvider.ReadStream(AvailableBuffers[unqueuedBufferIndex]);
                if (!eof) {
                    QueuedBuffers.Enqueue(unqueuedBufferIndex);
                } else {
                    strStopReq = StopRequest.EndOfStream;
                }
            }
        }
    }
}