using Jazz2.Game.Structs;

namespace Jazz2.Actors
{
    public class Checkpoint : ActorBase
    {
        private bool activated;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            canBeFrozen = false;

            RequestMetadata("Object/Checkpoint");

            SetAnimation("Closed");
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(20, 20);
        }

        public override void OnHandleCollision(ActorBase other)
        {
            switch (other) {
                case Player player: {
                    if (!activated) {
                        activated = true;

                        player.SetCheckpoint(Transform.Pos.Xy);

                        SetAnimation("Opened");
                        SetTransition(AnimState.TransitionActivate, false);

                        PlaySound("TransitionActivate");
                    }
                    break;
                }
            }
        }
    }
}