using Jazz2.Game.Structs;

namespace Jazz2.Actors.Collectibles
{
    public class FoodCollectible : Collectible
    {
        private bool isDrinkable;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            scoreValue = 50;

            SetAnimation("Food" + ((FoodType)details.Params[0]).ToString("G"));

            switch ((FoodType)details.Params[0]) {
                case FoodType.Pepsi:
                case FoodType.Coke:
                case FoodType.Milk:
                    isDrinkable = true;
                    break;
                default:
                    isDrinkable = false;
                    break;
            }

            SetFacingDirection();
        }

        protected override void Collect(Player player)
        {
            player.ConsumeFood(isDrinkable);

            base.Collect(player);
        }
    }
}