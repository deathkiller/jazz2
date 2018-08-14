namespace Jazz2.Actors.Collectibles
{
    public class CarrotFlyCollectible : Collectible
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            scoreValue = 500;

            SetAnimation("CarrotFly");

            SetFacingDirection();
        }

        protected override void Collect(Player player)
        {
            if (player.SetModifier(Player.Modifier.Copter)) {
                base.Collect(player);
            }
        }
    }
}