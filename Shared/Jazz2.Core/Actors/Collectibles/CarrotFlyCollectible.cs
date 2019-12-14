using System.Threading.Tasks;

namespace Jazz2.Actors.Collectibles
{
    public class CarrotFlyCollectible : Collectible
    {
        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await base.OnActivatedAsync(details);

            scoreValue = 500;

            await RequestMetadataAsync("Collectible/CarrotFly");
            SetAnimation("Carrot");

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