namespace Jazz2.Actors.Collectibles
{
    public class OneUpCollectible : Collectible
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            scoreValue = 1000;

            SetAnimation("OneUp");
        }

        protected override void Collect(Player player)
        {
            player.AddLives(1);

            base.Collect(player);
        }
    }
}