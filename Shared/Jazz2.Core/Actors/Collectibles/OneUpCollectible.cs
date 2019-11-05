using System.Threading.Tasks;

namespace Jazz2.Actors.Collectibles
{
    public class OneUpCollectible : Collectible
    {
        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await base.OnActivatedAsync(details);

            scoreValue = 1000;

            await RequestMetadataAsync("Collectible/OneUp");
            SetAnimation("OneUp");
        }

        protected override void Collect(Player player)
        {
            player.AddLives(1);

            base.Collect(player);
        }
    }
}