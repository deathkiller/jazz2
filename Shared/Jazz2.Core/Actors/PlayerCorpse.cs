using System.Threading.Tasks;

namespace Jazz2.Actors
{
    public class PlayerCorpse : ActorBase
    {
        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            PlayerType playerType = (PlayerType)details.Params[0];
            IsFacingLeft = (details.Params[1] != 0);

            switch (playerType) {
                default:
                case PlayerType.Jazz:
                    await RequestMetadataAsync("Interactive/PlayerJazz");
                    break;
                case PlayerType.Spaz:
                    await RequestMetadataAsync("Interactive/PlayerSpaz");
                    break;
                case PlayerType.Lori:
                    await RequestMetadataAsync("Interactive/PlayerLori");
                    break;
            }

            SetAnimation("Corpse");

            collisionFlags = CollisionFlags.ForceDisableCollisions;
        }

        public override void OnFixedUpdate(float timeMult)
        {
        }
    }
}