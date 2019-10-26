using System.Threading.Tasks;
using Duality;
using Jazz2.Actors;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;
using Lidgren.Network;

namespace Jazz2.Game.Multiplayer
{
    public class RemotePlayer : ActorBase
    {
        private struct StateFrame
        {
            public double Time;
            public Vector2 Pos;
        }

        private const double ServerDelay = 2 * 1.0 / 30; // 66ms (2 server updates) delay to allow better interpolation

        public int PlayerIndex;
        public PlayerType PlayerType;

        private StateFrame[] stateBuffer = new StateFrame[6];
        private int stateBufferPos = 0;
        private float posZ;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            PlayerType = (PlayerType)details.Params[0];
            PlayerIndex = details.Params[1];

            double timeNow = NetTime.Now;
            for (int i = 0; i < stateBuffer.Length; i++) {
                stateBuffer[i].Time = timeNow - stateBuffer.Length + i;
                stateBuffer[i].Pos = details.Pos.Xy;
            }
            posZ = details.Pos.Z;

            health = int.MaxValue;

            switch (PlayerType) {
                case PlayerType.Jazz:
                    await RequestMetadataAsync("Interactive/PlayerJazz");
                    break;
                case PlayerType.Spaz:
                    await RequestMetadataAsync("Interactive/PlayerSpaz");
                    break;
                case PlayerType.Lori:
                    await RequestMetadataAsync("Interactive/PlayerLori");
                    break;
                case PlayerType.Frog:
                    await RequestMetadataAsync("Interactive/PlayerFrog");
                    break;
            }

            SetAnimation(AnimState.Fall);

            collisionFlags = CollisionFlags.CollideWithOtherActors;
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

        public void UpdateFromServer(Vector3 pos, AnimState animState, float animTime, bool isFacingLeft)
        {
            stateBuffer[stateBufferPos].Time = NetTime.Now;
            stateBuffer[stateBufferPos].Pos = pos.Xy;
            stateBufferPos++;
            if (stateBufferPos >= stateBuffer.Length) {
                stateBufferPos = 0;
            }
            posZ = pos.Z;

            if (availableAnimations != null) {
                if (currentAnimationState != animState) {
                    SetAnimation(animState);
                }

                if (animTime < 0) {
                    renderer.Active = false;
                } else {
                    renderer.Active = true;
                    renderer.AnimTime = animTime;
                    IsFacingLeft = isFacingLeft;
                }
            }
        }

        public override void OnHandleCollision(ActorBase other)
        {
            switch (other) {
                case AmmoBase ammo: {
                    if ((ammo.Index & 0xff) != PlayerIndex) {
                        api.BroadcastTriggeredEvent(EventType.ModifierHurt, new ushort[] { (ushort)PlayerIndex, 1 });
                        ammo.DecreaseHealth(int.MaxValue);
                    }
                    break;
                }
            }
        }
    }
}