namespace Jazz2.Actors
{
    partial class Player
    {
        public SpecialMoveType CurrentSpecialMove => currentSpecialMove;

#if MULTIPLAYER && SERVER
        //public AnimState AnimState => currentAnimationState;

        public void SyncWithClient(SpecialMoveType specialMove, bool isFacingLeft)
        {
            //currentAnimationState = animState;
            currentSpecialMove = specialMove;
            IsFacingLeft = isFacingLeft;
        }

        public void OnRefreshActorAnimation(string identifier)
        {
            SetAnimation(identifier);

            OnUpdateHitbox();
        }
#endif
    }
}