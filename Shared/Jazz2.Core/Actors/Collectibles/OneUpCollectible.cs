using System.Threading.Tasks;

namespace Jazz2.Actors.Collectibles
{
    public class OneUpCollectible : Collectible
    {
        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Collectible/OneUp");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new OneUpCollectible();
            actor.OnActivated(details);
            return actor;
        }

        private OneUpCollectible()
        {
        }

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