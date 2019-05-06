using Duality;
using Jazz2.Game.Structs;
using Jazz2.Networking.Packets.Client;

namespace Jazz2.Actors
{
    partial class Player
    {
        public UpdateSelf CreateUpdatePacket()
        {
            return new UpdateSelf {
                Pos = Transform.Pos,
                Speed = new Vector2(speedX, speedY),

                AnimState = (currentTransitionState != AnimState.Idle ? currentTransitionState : currentAnimationState),
                AnimTime = (renderer.Active ? renderer.AnimTime : -1),
                IsFacingLeft = IsFacingLeft,

                Controllable = controllable,
                IsFirePressed = wasFirePressed
            };
        }
    }
}