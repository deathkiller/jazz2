using Duality;

namespace Jazz2.Actors.Collectibles
{
    public class CarrotInvincibleCollectible : Collectible
    {
        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

            scoreValue = 500;

            RequestMetadata("Collectible/CarrotInvincible");
            SetAnimation("Carrot");

            SetFacingDirection();
        }

        protected override void Collect(Player player)
        {
            player.SetInvulnerability(30 * Time.FramesPerSecond, true);

            base.Collect(player);
        }
    }
}