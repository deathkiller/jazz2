using Duality;
using Duality.Audio;
using Duality.Components;
using Jazz2.Game;
using Jazz2.Game.Structs;
using Lidgren.Network;

namespace Jazz2.Actors
{
    public class RemoteActor : ActorBase
    {
        private struct StateFrame
        {
            public double Time;
            public Vector2 Pos;
        }

        private const double ServerDelay = 2 * 1.0 / 30; // ~66ms (2 server updates) delay to allow better interpolation

        public int PlayerIndex;
        public PlayerType PlayerType;

        private StateFrame[] stateBuffer = new StateFrame[6];
        private int stateBufferPos = 0;
        private float posZ;

        public async void OnActivated(ILevelHandler levelHandler, Vector3 pos, string metadataPath, CollisionFlags collisionFlags)
        {
            initState = InitState.Initializing;

            this.levelHandler = levelHandler;
            this.flags = ActorInstantiationFlags.None;
            this.CollisionFlags = (collisionFlags & ~CollisionFlags.ApplyGravitation);

            double timeNow = NetTime.Now;
            for (int i = 0; i < stateBuffer.Length; i++) {
                stateBuffer[i].Time = timeNow - stateBuffer.Length + i;
                stateBuffer[i].Pos = pos.Xy;
            }
            posZ = pos.Z;

            health = int.MaxValue;

            friction = 1.5f;

            originTile = new Point2((int)(pos.X / 32), (int)(pos.Y / 32));

            Transform transform = AddComponent<Transform>();
            transform.Pos = pos;

            AddComponent(new LocalController(this));

            //await OnActivatedAsync(details);

            await RequestMetadataAsync(metadataPath);

            if (initState == InitState.Initializing) {
                initState = InitState.Initialized;
            }
        }

        public override void OnUpdate()
        {
            double timeNow = NetTime.Now;
            double renderTime = timeNow - ServerDelay;

            int nextIdx = stateBufferPos - 1;
            if (nextIdx < 0) {
                nextIdx += stateBuffer.Length;
            }

            if (renderTime <= stateBuffer[nextIdx].Time) {
                int prevIdx;
                while (true) {
                    prevIdx = nextIdx - 1;
                    if (prevIdx < 0) {
                        prevIdx += stateBuffer.Length;
                    }

                    if (prevIdx == stateBufferPos || stateBuffer[prevIdx].Time <= renderTime) {
                        break;
                    }

                    nextIdx = prevIdx;
                }

                Vector2 pos;
                double timeRange = (stateBuffer[nextIdx].Time - stateBuffer[prevIdx].Time);
                if (timeRange > 0) {
                    pos = stateBuffer[prevIdx].Pos + (stateBuffer[nextIdx].Pos - stateBuffer[prevIdx].Pos) * (float)((renderTime - stateBuffer[prevIdx].Time) / timeRange);
                } else {
                    pos = stateBuffer[nextIdx].Pos;
                }

                Transform.Pos = new Vector3(pos, posZ);
            }

            base.OnUpdate();
        }

        public void SyncWithServer(bool isVisible, Vector3 pos, bool isFacingLeft)
        {
            bool wasVisible;
            if (availableAnimations != null && renderer != null) {
                wasVisible = !renderer.AnimHidden;

                renderer.AnimHidden = !isVisible;
                IsFacingLeft = isFacingLeft;
            } else {
                wasVisible = false;
            }

            if (wasVisible) {
                // Actor is still visible, enable interpolation
                stateBuffer[stateBufferPos].Time = NetTime.Now;
                stateBuffer[stateBufferPos].Pos = pos.Xy;
            } else {
                // Actor was hidden before, reset state buffer to disable interpolation
                int stateBufferPrevPos = stateBufferPos - 1;
                if (stateBufferPrevPos < 0) {
                    stateBufferPrevPos += stateBuffer.Length;
                }

                double time = NetTime.Now - ServerDelay;

                stateBuffer[stateBufferPrevPos].Time = time;
                stateBuffer[stateBufferPrevPos].Pos = pos.Xy;
                stateBuffer[stateBufferPos].Time = time;
                stateBuffer[stateBufferPos].Pos = pos.Xy;
            }

            stateBufferPos++;
            if (stateBufferPos >= stateBuffer.Length) {
                stateBufferPos = 0;
            }

            posZ = pos.Z;
        }

        public void SyncWithServer(bool isVisible)
        {
            if (availableAnimations != null && renderer != null) {
                renderer.AnimHidden = !isVisible;
            }
        }

        public void OnRefreshActorAnimation(string identifier)
        {
            SetAnimation(identifier);

            OnUpdateHitbox();
        }

        public void OnPlaySound(string soundName, Vector3 pos, float gain, float pitch, float lowpass)
        {
#if !DISABLE_SOUND
            if (availableSounds.TryGetValue(soundName, out SoundResource resource)) {
                SoundInstance instance;
                if (pos == default(Vector3)) {
                    instance = DualityApp.Sound.PlaySound3D(resource.Sound, this);
                } else {
                    instance = DualityApp.Sound.PlaySound3D(resource.Sound, pos);
                }
                instance.Flags |= SoundInstanceFlags.GameplaySpecific;

                // ToDo: Hardcoded volume
                instance.Volume = gain * SettingsCache.SfxVolume;

                instance.Pitch = pitch;
                instance.Lowpass = lowpass;
            }
#endif
        }
    }
}