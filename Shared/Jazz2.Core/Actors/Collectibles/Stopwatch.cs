using System.Threading.Tasks;

namespace Jazz2.Actors.Collectibles
{
    public class Stopwatch : Collectible
    {
        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Collectible/Stopwatch");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new Stopwatch();
            actor.OnActivated(details);
            return actor;
        }

        private Stopwatch()
        {
        }

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