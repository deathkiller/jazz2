using System.Threading.Tasks;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Collectibles
{
    public class FastFireCollectible : Collectible
    {
        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await base.OnActivatedAsync(details);

            scoreValue = 200;

            PlayerType player = (levelHandler.Players.Count == 0 ? PlayerType.Jazz : levelHandler.Players[0].PlayerType);
            if (player == PlayerType.Spaz) {
                await RequestMetadataAsync("Collectible/FastFireSpaz");
            } else if (player == PlayerType.Lori) {
                await RequestMetadataAsync("Collectible/FastFireLori");
            } else {
                await RequestMetadataAsync("Collectible/FastFireJazz");
            }

            SetAnimation("FastFire");

            SetFacingDirection();
        }

        protected override void Collect(Player player)
        {
            if (player.AddFastFire(1)) {
                base.Collect(player);
            }
        }
    }
}