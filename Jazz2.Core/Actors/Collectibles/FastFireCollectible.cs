namespace Jazz2.Actors.Collectibles
{
    public class FastFireCollectible : Collectible
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            scoreValue = 200;

            PlayerType player = (api.Players.Count == 0 ? PlayerType.Jazz : api.Players[0].PlayerType);
            if (player == PlayerType.Spaz) {
                SetAnimation("FastFireSpaz");
            } else if (player == PlayerType.Lori) {
                SetAnimation("FastFireLori");
            } else {
                SetAnimation("FastFireJazz");
            }

            SetFacingDirection();
        }

        protected override void Collect(Player player)
        {
            if (player.AddFastFire(1)) {
                base.Collect(player);
            }
        }
    }
}