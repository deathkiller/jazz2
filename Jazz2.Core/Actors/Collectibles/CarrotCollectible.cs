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
                SetAnimation("CarrotFull");
            } else {
                scoreValue = 200;
                SetAnimation("Carrot");
            }

            SetFacingDirection();
        }

        public override void Collect(Player player) {
            if (player.AddHealth(maxCarrot ? -1 : 1)) {
                base.Collect(player);
            }
        }
    }
}