namespace Jazz2.Actors.Collectibles
{
    public class OneUpCollectible : Collectible
    {
        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

            scoreValue = 1000;

            RequestMetadata("Collectible/OneUp");
            SetAnimation("OneUp");
        }

        protected override void Collect(Player player)
        {
            player.AddLives(1);

            base.Collect(player);
        }
    }
}