using Jazz2.Game.Structs;
using Jazz2.Networking.Packets.Client;

namespace Jazz2.Actors
{
    partial class Player
    {
#if SERVER
        public AnimState AnimState => currentAnimationState;

        public void SyncWithClient(AnimState animState, bool isFacingLeft)
        {
            currentAnimationState = animState;
            IsFacingLeft = isFacingLeft;
        }
#endif

        public UpdateSelf CreateUpdatePacket()
        {
            return new UpdateSelf {
                Pos = Transform.Pos,

                AnimState = (currentTransitionState != AnimState.Idle ? currentTransitionState : currentAnimationState),
                //AnimTime = (renderer.Active ? renderer.AnimTime : -1),
                IsFacingLeft = IsFacingLeft,

                //Controllable = controllable,
                IsFirePressed = wasFirePressed
            };
        }
    }
}