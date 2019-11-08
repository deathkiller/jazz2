using Duality;
using Duality.Components;
using Jazz2.Game;
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

                Vector2 pos = stateBuffer[prevIdx].Pos + (stateBuffer[nextIdx].Pos - stateBuffer[prevIdx].Pos) * (float)((renderTime - stateBuffer[prevIdx].Time) / (stateBuffer[nextIdx].Time - stateBuffer[prevIdx].Time));
                Transform.Pos = new Vector3(pos, posZ);
            }

            base.OnUpdate();
        }

        public void SyncWithServer(bool visible, Vector3 pos, bool isFacingLeft)
        {
            int prevIdx = stateBufferPos - 1;
            if (prevIdx < 0) {
                prevIdx += stateBuffer.Length;
            }

            if (MathF.Abs(pos.X - stateBuffer[prevIdx].Pos.X) >= 2f ||
                MathF.Abs(pos.Y - stateBuffer[prevIdx].Pos.Y) >= 2f) {
                stateBuffer[stateBufferPos].Time = NetTime.Now;
                stateBuffer[stateBufferPos].Pos = pos.Xy;
                stateBufferPos++;
                if (stateBufferPos >= stateBuffer.Length) {
                    stateBufferPos = 0;
                }
            }

            posZ = pos.Z;

            if (availableAnimations != null && renderer != null) {
                renderer.Active = visible;
                IsFacingLeft = isFacingLeft;
            }
        }

        public void SyncWithServer(bool visible)
        {
            if (availableAnimations != null && renderer != null) {
                renderer.Active = visible;
            }
        }

        public void OnRefreshActorAnimation(string identifier)
        {
            SetAnimation(identifier);

            OnUpdateHitbox();
        }
    }
}