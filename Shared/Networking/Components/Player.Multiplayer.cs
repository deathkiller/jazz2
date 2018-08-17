using Jazz2.Game.Structs;
using Jazz2.Networking.Packets.Client;

namespace Jazz2.Actors
{
    partial class Player
    {
        public UpdateSelf CreateUpdatePacket()
        {
            return new UpdateSelf {
                //Index = playerIndex,

                Pos = Transform.Pos,
                Speed = Speed,

                AnimState = (currentTransitionState != AnimState.Idle ? currentTransitionState : currentAnimationState),
                AnimTime = renderer.AnimTime,
                IsFacingLeft = IsFacingLeft
            };
        }
    }
}