using Jazz2.Game.Structs;

namespace Jazz2.Actors
{
    public class SavePoint : ActorBase
    {
        private bool activated;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            canBeFrozen = false;

            RequestMetadata("Object/SavePoint");

            SetAnimation("Closed");
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(20, 20);
        }

        public bool ActivateSavePoint()
        {
            if (!activated) {
                activated = true;

                SetAnimation("Opened");
                SetTransition(AnimState.TransitionActivate, false);

                PlaySound("TransitionActivate");

                return true;
            } else {
                return false;
            }
        }
    }
}