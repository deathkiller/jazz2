using System.Threading.Tasks;

namespace Jazz2.Actors.Collectibles
{
    public class Stopwatch : Collectible
    {
        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await base.OnActivatedAsync(details);

            await RequestMetadataAsync("Collectible/Stopwatch");
            SetAnimation("Stopwatch");

            SetFacingDirection();
        }

        protected override void Collect(Player player)
        {
            if (player.IncreaseShieldTime(10f)) {
                base.Collect(player);
            }
        }
    }
}