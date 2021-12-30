using System.Threading.Tasks;

namespace Jazz2.Actors.Collectibles
{
    public class CoinCollectible : Collectible
    {
        private int coinValue;

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Collectible/Coins");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new CoinCollectible();
            actor.OnActivated(details);
            return actor;
        }

        private CoinCollectible()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await base.OnActivatedAsync(details);

            ushort coinType = details.Params[0];

            await RequestMetadataAsync("Collectible/Coins");

            switch (coinType) {
                default:
                case 0: // Silver
                    coinValue = 1;
                    scoreValue = 500;
                    SetAnimation("CoinSilver");
                    break;
                case 1: // Gold
                    coinValue = 5;
                    scoreValue = 1000;
                    SetAnimation("CoinGold");
                    break;
            }

            SetFacingDirection();
        }

        protected override void Collect(Player player)
        {
            player.AddCoins(coinValue);

            base.Collect(player);
        }
    }
}