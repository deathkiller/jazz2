using System.Threading.Tasks;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Bosses
{
    public class Tweedle : BossBase
    {
        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Boss/Tweedle");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new Tweedle();
            actor.OnActivated(details);
            return actor;
        }

        private Tweedle()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await RequestMetadataAsync("Boss/Tweedle");
            SetAnimation(AnimState.Idle);
        }

        // ToDo: Implement this
        // https://www.jazz2online.com/wiki/Tweedle_Boss
    }
}