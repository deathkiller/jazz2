﻿using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Crab : EnemyBase
    {
        private const float DefaultSpeed = 0.7f;

        private float noiseCooldown = 80f;
        private float stepCooldown = 8f;
        private bool canJumpPrev;
        private bool stuck;

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Enemy/Crab");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new Crab();
            actor.OnActivated(details);
            return actor;
        }

        public Crab()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            SetHealthByDifficulty(3);
            scoreValue = 300;

            await RequestMetadataAsync("Enemy/Crab");
            SetAnimation(AnimState.Fall);

            IsFacingLeft = MathF.Rnd.NextBool();
            speedX = (IsFacingLeft ? -1 : 1) * DefaultSpeed;

            canJumpPrev = canJump;
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(26, 20);
        }

        public override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            if (frozenTimeLeft > 0) {
                return;
            }

            if (canJump) {
                if (!canJumpPrev) {
                    canJumpPrev = true;
                    SetAnimation(AnimState.Walk);
                    SetTransition(AnimState.TransitionFallToIdle, false);
                }

                if (!CanMoveToPosition(speedX * 4, 0)) {
                    if (stuck) {
                        MoveInstantly(new Vector2(0f, -2f), MoveType.Relative, true);
                    } else {
                        IsFacingLeft = !IsFacingLeft;
                        speedX = (IsFacingLeft ? -1 : 1) * DefaultSpeed;
                        stuck = true;
                    }
                } else {
                    stuck = false;
                }

                if (noiseCooldown <= 0f) {
                    noiseCooldown = MathF.Rnd.NextFloat(60, 160);
                    PlaySound("Noise", 0.4f);
                } else {
                    noiseCooldown -= timeMult;
                }

                if (stepCooldown <= 0f) {
                    stepCooldown = MathF.Rnd.NextFloat(7, 10);
                    PlaySound("Step", 0.15f);
                } else {
                    stepCooldown -= timeMult;
                }
            } else {
                if (canJumpPrev) {
                    canJumpPrev = false;
                    SetAnimation(AnimState.Fall);
                }
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);
            levelHandler.PlayCommonSound("Splat", Transform.Pos);

            Explosion.Create(levelHandler, Transform.Pos, Explosion.Large);

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}