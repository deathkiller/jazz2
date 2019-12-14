using System.Threading.Tasks;
using Duality;

namespace Jazz2.Actors.Collectibles
{
    public class CarrotInvincibleCollectible : Collectible
    {
        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await base.OnActivatedAsync(details);

            scoreValue = 500;

            await RequestMetadataAsync("Collectible/CarrotInvincible");
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