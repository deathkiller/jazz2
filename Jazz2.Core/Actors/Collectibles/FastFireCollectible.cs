namespace Jazz2.Actors.Collectibles
{
    public class FastFireCollectible : Collectible
    {
        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

            scoreValue = 200;

            PlayerType player = (api.Players.Count == 0 ? PlayerType.Jazz : api.Players[0].PlayerType);
            if (player == PlayerType.Spaz) {
                RequestMetadata("Collectible/FastFireSpaz");
            } else if (player == PlayerType.Lori) {
                RequestMetadata("Collectible/FastFireLori");
            } else {
                RequestMetadata("Collectible/FastFireJazz");
            }

            SetAnimation("FastFire");

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