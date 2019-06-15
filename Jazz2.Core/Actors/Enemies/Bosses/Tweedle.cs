using System.Threading.Tasks;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Bosses
{
    public class Tweedle : BossBase
    {
        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await RequestMetadataAsync("Boss/Tweedle");
            SetAnimation(AnimState.Idle);
        }

        // ToDo: Implement this
        // https://www.jazz2online.com/wiki/Tweedle_Boss
    }
}