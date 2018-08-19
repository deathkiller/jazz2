namespace Jazz2.Actors.Collectibles
{
    public class GemCollectible : Collectible
    {
        private ushort gemType;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            gemType = (ushort)(details.Params[0] & 0x3);

            RequestMetadata("Collectible/Gems");

            switch (gemType) {
                case 0: // Red (+1)
                    scoreValue = 100;
                    SetAnimation("GemRed");
                    break;
                case 1: // Green (+5)
                    scoreValue = 500;
                    SetAnimation("GemGreen");
                    break;
                case 2: // Blue (+10)
                    scoreValue = 1000;
                    SetAnimation("GemBlue");
                    break;
                case 3: // Purple
                    scoreValue = 100;
                    SetAnimation("GemPurple");
                    break;
            }

            SetFacingDirection();
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(18, 18);
        }

        protected override void Collect(Player player)
        {
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