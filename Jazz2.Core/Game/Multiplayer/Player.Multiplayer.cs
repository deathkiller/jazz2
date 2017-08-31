#if MULTIPLAYER

using Jazz2.Game.Structs;

namespace Jazz2.Actors
{
    partial class Player
    {
        public AnimState ActiveAnimState => (currentTransitionState != AnimState.Idle ? currentTransitionState : currentAnimationState);
        public float ActimeAnimTime => renderer.AnimTime;
        public bool IsFacingLeft => isFacingLeft;
    }
}

#endif