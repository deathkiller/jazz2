namespace Jazz2.Actors.Collectibles
{
    public class CarrotFlyCollectible : Collectible
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            scoreValue = 500;

            SetAnimation("PICKUP_CARROT_FLY");

            SetFacingDirection();
        }

        public override void Collect(Player player)
        {
            base.Collect(player);

            // ToDo: Implement flying carrots
        }
    }
}