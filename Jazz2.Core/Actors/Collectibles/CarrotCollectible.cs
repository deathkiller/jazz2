using Duality;

namespace Jazz2.Actors.Collectibles
{
    public class CarrotCollectible : Collectible
    {
        private bool maxCarrot;

        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

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
            if (maxCarrot) {
                player.AddHealth(-1);
                player.SetInvulnerability(4 * Time.FramesPerSecond, true);
                base.Collect(player);
            } else {
                if (player.AddHealth(1)) {
                    //player.SetInvulnerability(1 * Time.FramesPerSecond, true);
                    base.Collect(player);
                }
            }
        }
    }
}