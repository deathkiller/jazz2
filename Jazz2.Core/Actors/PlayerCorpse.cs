namespace Jazz2.Actors
{
    public class PlayerCorpse : ActorBase
    {
        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

            PlayerType playerType = (PlayerType)details.Params[0];
            IsFacingLeft = (details.Params[1] != 0);

            switch (playerType) {
                default:
                case PlayerType.Jazz:
                    RequestMetadata("Interactive/PlayerJazz");
                    break;
                case PlayerType.Spaz:
                    RequestMetadata("Interactive/PlayerSpaz");
                    break;
                case PlayerType.Lori:
                    RequestMetadata("Interactive/PlayerLori");
                    break;
            }

            SetAnimation("Corpse");

            collisionFlags = CollisionFlags.ForceDisableCollisions;
        }

        protected override void OnUpdate()
        {
            // Nothing to do...
        }
    }
}