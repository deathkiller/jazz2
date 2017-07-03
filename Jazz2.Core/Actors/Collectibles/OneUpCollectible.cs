namespace Jazz2.Actors.Collectibles
{
    public class OneUpCollectible : Collectible
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            scoreValue = 1000;

            SetAnimation("PICKUP_ONEUP");
        }

        public override void Collect(Player player) {
            player.AddLives(1);

            base.Collect(player);
        }
    }
}