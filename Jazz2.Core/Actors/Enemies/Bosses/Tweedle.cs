using Jazz2.Game.Structs;

namespace Jazz2.Actors.Bosses
{
    public class Tweedle : BossBase
    {
        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

            RequestMetadata("Boss/Tweedle");
            SetAnimation(AnimState.Idle);
        }

        // ToDo: Implement this
        // https://www.jazz2online.com/wiki/Tweedle_Boss
    }
}