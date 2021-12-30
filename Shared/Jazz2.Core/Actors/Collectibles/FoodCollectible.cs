using System.Threading.Tasks;

namespace Jazz2.Actors.Collectibles
{
    public class FoodCollectible : Collectible
    {
        public enum FoodType
        {
            Apple = 1,
            Banana = 2,
            Cherry = 3,
            Orange = 4,
            Pear = 5,
            Pretzel = 6,
            Strawberry = 7,
            Lemon = 8,
            Lime = 9,
            Thing = 10,
            WaterMelon = 11,
            Peach = 12,
            Grapes = 13,
            Lettuce = 14,
            Eggplant = 15,
            Cucumber = 16,
            Pepsi = 17,
            Coke = 18,
            Milk = 19,
            Pie = 20,
            Cake = 21,
            Donut = 22,
            Cupcake = 23,
            Chips = 24,
            Candy = 25,
            Chocolate = 26,
            IceCream = 27,
            Burger = 28,
            Pizza = 29,
            Fries = 30,
            ChickenLeg = 31,
            Sandwich = 32,
            Taco = 33,
            HotDog = 34,
            Ham = 35,
            Cheese = 36,
        }

        private bool isDrinkable;

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Collectible/Food" + ((FoodType)details.Params[0]).ToString("G"));
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new FoodCollectible();
            actor.OnActivated(details);
            return actor;
        }

        private FoodCollectible()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await base.OnActivatedAsync(details);

            scoreValue = 50;

            await RequestMetadataAsync("Collectible/Food" + ((FoodType)details.Params[0]).ToString("G"));
            SetAnimation("Food");

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