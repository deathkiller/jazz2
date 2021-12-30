﻿using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Environment
{
    public class Eva : ActorBase
    {
        private float animationTime;

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Object/Eva");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new Eva();
            actor.OnActivated(details);
            return actor;
        }

        private Eva()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await RequestMetadataAsync("Object/Eva");
            SetAnimation(AnimState.Idle);

            CollisionFlags &= ~CollisionFlags.ApplyGravitation;
        }

        public override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            if (currentTransitionState == AnimState.Idle) {
                if (animationTime <= 0f) {
                    SetTransition(AnimState.TransitionIdleBored, true);
                    animationTime = MathF.Rnd.NextFloat(160f, 200f);
                } else {
                    animationTime -= timeMult;
                }
            }
        }

        public override void OnHandleCollision(ActorBase other)
        {
            switch (other) {
                case Player player: {
                    if (player.PlayerType == PlayerType.Frog && player.DisableControllable(160f)) {
                        SetTransition(AnimState.TransitionAttack, false, delegate {
                            player.MorphRevent();

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