namespace Jazz2.Actors.Collectibles
{
    public class CarrotCollectible : Collectible
    {
        private bool maxCarrot;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            maxCarrot = (details.Params[0] != 0);

            if (maxCarrot) {
                scoreValue = 500;
                RequestMetadata("Collectible/CarrotFull");
            } else {
                scoreValue = 200;
                RequestMetadata("Collectible/Carrot");
            }

            SetAnimation("Carrot");

            SetFacingDirection();
        }

        protected override void Collect(Player player)
        {
            if (player.AddHealth(maxCarrot ? -1 : 1)) {
                base.Collect(player);
            }
        }
    }
}