using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors
{
    public class Eva : ActorBase
    {
        private float animationTime;

        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

            RequestMetadata("Object/Eva");
            SetAnimation(AnimState.Idle);

            collisionFlags &= ~CollisionFlags.ApplyGravitation;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (currentTransitionState == AnimState.Idle) {
                if (animationTime <= 0f) {
                    SetTransition(AnimState.TransitionIdleBored, true);
                    animationTime = MathF.Rnd.NextFloat(160f, 200f);
                } else {
                    animationTime -= Time.TimeMult;
                }
            }
        }

        public override void OnHandleCollision(ActorBase other)
        {
            switch (other) {
                case Player player: {
                    if (player.PlayerType == PlayerType.Frog && player.DisableControllableWithTimeout(160f)) {
                        SetTransition(AnimState.TransitionAttack, false, delegate {
                            player.MorphToOriginal();

                            PlaySound("Kiss", 0.8f);
                            SetTransition(AnimState.TransitionAttackEnd, false);
                        });
                    }
                    break;
                }
            }
        }
    }
}