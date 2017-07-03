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

            switch ((EventType)details.Params[0]) {
                case EventType.FoodApple: SetAnimation("PICKUP_FOOD_APPLE"); break;
                case EventType.FoodBanana: SetAnimation("PICKUP_FOOD_BANANA"); break;
                case EventType.FoodCherry: SetAnimation("PICKUP_FOOD_CHERRY"); break;
                case EventType.FoodOrange: SetAnimation("PICKUP_FOOD_ORANGE"); break;
                case EventType.FoodPear: SetAnimation("PICKUP_FOOD_PEAR"); break;
                case EventType.FoodPretzel: SetAnimation("PICKUP_FOOD_PRETZEL"); break;
                case EventType.FoodStrawberry: SetAnimation("PICKUP_FOOD_STRAWBERRY"); break;
                case EventType.FoodLemon: SetAnimation("PICKUP_FOOD_LEMON"); break;
                case EventType.FoodLime: SetAnimation("PICKUP_FOOD_LIME"); break;
                case EventType.FoodThing: SetAnimation("PICKUP_FOOD_THING"); break;
                case EventType.FoodWaterMelon: SetAnimation("PICKUP_FOOD_WATERMELON"); break;
                case EventType.FoodPeach: SetAnimation("PICKUP_FOOD_PEACH"); break;
                case EventType.FoodGrapes: SetAnimation("PICKUP_FOOD_GRAPES"); break;
                case EventType.FoodLettuce: SetAnimation("PICKUP_FOOD_LETTUCE"); break;
                case EventType.FoodEggplant: SetAnimation("PICKUP_FOOD_EGGPLANT"); break;
                case EventType.FoodCucumber: SetAnimation("PICKUP_FOOD_CUCUMBER"); break;
                case EventType.FoodPepsi: SetAnimation("PICKUP_FOOD_SODA"); break;
                case EventType.FoodCoke: SetAnimation("PICKUP_FOOD_COLA"); break;
                case EventType.FoodMilk: SetAnimation("PICKUP_FOOD_MILK"); break;
                case EventType.FoodPie: SetAnimation("PICKUP_FOOD_PIE"); break;
                case EventType.FoodCake: SetAnimation("PICKUP_FOOD_CAKE"); break;
                case EventType.FoodDonut: SetAnimation("PICKUP_FOOD_DONUT"); break;
                case EventType.FoodCupcake: SetAnimation("PICKUP_FOOD_CUPCAKE"); break;
                case EventType.FoodChips: SetAnimation("PICKUP_FOOD_CHIPS"); break;
                case EventType.FoodCandy: SetAnimation("PICKUP_FOOD_CANDY"); break;
                case EventType.FoodChocolate: SetAnimation("PICKUP_FOOD_CHOCOLATE"); break;
                case EventType.FoodIceCream: SetAnimation("PICKUP_FOOD_ICECREAM"); break;
                case EventType.FoodBurger: SetAnimation("PICKUP_FOOD_BURGER"); break;
                case EventType.FoodPizza: SetAnimation("PICKUP_FOOD_PIZZA"); break;
                case EventType.FoodFries: SetAnimation("PICKUP_FOOD_FRIES"); break;
                case EventType.FoodChickenLeg: SetAnimation("PICKUP_FOOD_CHICKEN"); break;
                case EventType.FoodSandwich: SetAnimation("PICKUP_FOOD_SANDWICH"); break;
                case EventType.FoodTaco: SetAnimation("PICKUP_FOOD_TACO"); break;
                case EventType.FoodHotDog: SetAnimation("PICKUP_FOOD_HOTDOG"); break;
                case EventType.FoodHam: SetAnimation("PICKUP_FOOD_HAM"); break;
                case EventType.FoodCheese: SetAnimation("PICKUP_FOOD_CHEESE"); break;
                default:
                    break;
            }

            switch ((EventType)details.Params[0]) {
                case EventType.FoodPepsi:
                case EventType.FoodCoke:
                case EventType.FoodMilk:
                    isDrinkable = true;
                    break;
                default:
                    isDrinkable = false;
                    break;
            }

            SetFacingDirection();
        }

        public override void Collect(Player player) {
            player.ConsumeFood(isDrinkable);

            base.Collect(player);
        }
    }
}