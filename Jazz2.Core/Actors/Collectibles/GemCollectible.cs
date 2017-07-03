namespace Jazz2.Actors.Collectibles
{
    public class GemCollectible : Collectible
    {
        private ushort gemType;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            gemType = (ushort)(details.Params[0] & 0x3);

            switch (gemType) {
                case 0: // Red (+1)
                    scoreValue = 100;
                    SetAnimation("PICKUP_GEM_RED");
                    break;
                case 1: // Green (+5)
                    scoreValue = 500;
                    SetAnimation("PICKUP_GEM_GREEN");
                    break;
                case 2: // Blue (+10)
                    scoreValue = 1000;
                    SetAnimation("PICKUP_GEM_BLUE");
                    break;
                case 3: // Purple
                    scoreValue = 100;
                    SetAnimation("PICKUP_GEM_PURPLE");
                    break;
            }

            SetFacingDirection();
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(18, 18);
        }

        public override void Collect(Player player) {
            int value;
            switch (gemType) {
                default:
                case 0: // Red (+1)
                    value = 1;
                    break;
                case 1: // Green (+5)
                    value = 5;
                    break;
                case 2: // Blue (+10)
                    value = 10;
                    break;
                case 3: // Purple
                    value = 1;
                    break;
            }

            player.AddGems(value);

            base.Collect(player);
        }
    }
}