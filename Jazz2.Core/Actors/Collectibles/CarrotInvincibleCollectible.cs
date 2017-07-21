using Duality;

namespace Jazz2.Actors.Collectibles
{
    public class CarrotInvincibleCollectible : Collectible
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            scoreValue = 500;

            SetAnimation("CarrotInvincible");

            SetFacingDirection();
        }

        public override void Collect(Player player)
        {
            player.SetInvulnerability(30 * Time.FramesPerSecond);

            base.Collect(player);
        }
    }
}