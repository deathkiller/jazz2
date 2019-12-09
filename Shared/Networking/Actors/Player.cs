using Duality;

namespace Jazz2.Actors
{
    partial class Player
    {
        internal SpecialMoveType CurrentSpecialMove => currentSpecialMove;
        internal bool IsActivelyPushing => isActivelyPushing;

#if MULTIPLAYER && SERVER
        public void SyncWithClient(Vector3 speed, SpecialMoveType specialMove, bool isVisible, bool isFacingLeft, bool isActivelyPushing)
        {
            this.speedX = speed.X;
            this.speedY = speed.Y;

            this.currentSpecialMove = specialMove;

            //if (renderer != null) {
            //    renderer.AnimHidden = !isVisible;
            //}
            
            this.IsFacingLeft = isFacingLeft;
            this.isActivelyPushing = isActivelyPushing;
        }

        public void OnRefreshActorAnimation(string identifier)
        {
            //SetAnimation(identifier);

            //OnUpdateHitbox();

            levelHandler.BroadcastAnimationChanged(this, identifier);
        }
#endif
    }
}