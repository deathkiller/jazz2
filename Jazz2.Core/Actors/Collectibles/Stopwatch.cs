namespace Jazz2.Actors.Collectibles
{
    public class Stopwatch : Collectible
    {
        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

            RequestMetadata("Collectible/Stopwatch");
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