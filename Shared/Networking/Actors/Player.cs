namespace Jazz2.Actors
{
    partial class Player
    {
        internal SpecialMoveType CurrentSpecialMove => currentSpecialMove;
        internal bool IsActivelyPushing => isActivelyPushing;

#if MULTIPLAYER && SERVER
        public void SyncWithClient(SpecialMoveType specialMove, bool isFacingLeft, bool isActivelyPushing)
        {
            this.currentSpecialMove = specialMove;
            this.IsFacingLeft = isFacingLeft;
            this.isActivelyPushing = isActivelyPushing;
        }

        public void OnRefreshActorAnimation(string identifier)
        {
            SetAnimation(identifier);

            OnUpdateHitbox();
        }
#endif
    }
}