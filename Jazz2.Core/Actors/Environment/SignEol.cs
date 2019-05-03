using Jazz2.Game.Structs;

namespace Jazz2.Actors.Environment
{
    public class SignEol : ActorBase
    {
        private ExitType exitType;

        public ExitType ExitType => exitType;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            exitType = (ExitType)details.Params[0];

            canBeFrozen = false;

            RequestMetadata("Object/SignEol");
            SetAnimation("SignEol");
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(24, 24);
        }
    }
}