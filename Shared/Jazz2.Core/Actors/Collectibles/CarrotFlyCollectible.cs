using System.Threading.Tasks;

namespace Jazz2.Actors.Collectibles
{
    public class CarrotFlyCollectible : Collectible
    {
        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Collectible/CarrotFly");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new CarrotFlyCollectible();
            actor.OnActivated(details);
            return actor;
        }

        private CarrotFlyCollectible()
        {
        }

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