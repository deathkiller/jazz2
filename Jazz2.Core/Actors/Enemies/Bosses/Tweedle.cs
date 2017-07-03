using Jazz2.Game.Structs;

namespace Jazz2.Actors.Bosses
{
    public class Tweedle : BossBase
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            RequestMetadata("Boss/Tweedle");
            SetAnimation(AnimState.Idle);
        }

        // ToDo: Implement this
        // https://www.jazz2online.com/wiki/Tweedle_Boss
    }
}