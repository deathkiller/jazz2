using System.Threading.Tasks;
using Duality;

namespace Jazz2.Actors
{
    public class PlayerCorpse : ActorBase
    {
#if MULTIPLAYER && SERVER
        private float timeLeft = 3600; // 1 minute
#endif

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

            CollisionFlags = CollisionFlags.ForceDisableCollisions;
        }

        public override void OnFixedUpdate(float timeMult)
        {
#if MULTIPLAYER && SERVER
            timeLeft -= Time.TimeMult;
            if (timeLeft < 0f) {
                DecreaseHealth(int.MaxValue);
            }
#endif
        }
    }
}