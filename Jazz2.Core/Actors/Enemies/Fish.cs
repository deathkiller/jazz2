using System.Collections.Generic;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Fish : EnemyBase
    {
        private const int StateIdle = 0;
        private const int StateAttacking = 1;
        private const int StateBraking = 2;

        private int state = StateIdle;
        private float idleTime;
        private float attackCooldown = 60f;
        private Vector2 direction;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            collisionFlags &= ~CollisionFlags.ApplyGravitation;

            SetHealthByDifficulty(1);
            scoreValue = 100;

            RequestMetadata("Enemy/Fish");
            SetAnimation(AnimState.Idle);

            IsFacingLeft = MathF.Rnd.NextBool();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (frozenTimeLeft > 0) {
                return;
            }

            canJump = false;

            if (attackCooldown < 0f) {
                attackCooldown = 60f;

                Vector3 pos = Transform.Pos;
                Vector3 targetPos;

                List<Player> players = api.Players;
                for (int i = 0; i < players.Count; i++) {
                    targetPos = players[i].Transform.Pos;
                    direction = (targetPos.Xy - pos.Xy);
                    float length = direction.Length;
                    if (length < 320f) {
                        direction.Normalize();

                        speedX = speedY = 0f;
                        IsFacingLeft = (direction.X < 0f);
                        state = StateAttacking;

                        attackCooldown = 240f;

                        SetTransition(AnimState.TransitionAttack, false, delegate {
                            state = StateBraking;
                        });
                        break;
                    }
                }
            } else {
                float timeMult = Time.TimeMult;

                if (state == StateAttacking) {
                    speedX += direction.X * 0.12f * timeMult;
                    speedY += direction.Y * 0.12f * timeMult;
                } else if (state == StateBraking) {
                    speedX *= 0.96f / timeMult;
                    speedY *= 0.96f / timeMult;

                    if (MathF.Abs(speedX) < 0.01f && MathF.Abs(speedY) < 0.01f) {
                        state = StateIdle;
                    }
                } else {
                    if (idleTime < 0f) {
                        float x = MathF.Rnd.NextFloat(-1.4f, 1.4f);
                        float y = MathF.Rnd.NextFloat(-2f, 2f);

                        speedX = (speedX + x) * 0.2f;
                        speedY = (speedY + y) * 0.2f;

                        idleTime = 20f;
                    } else {
                        idleTime -= timeMult;
                    }
                }

                attackCooldown -= timeMult;
            }
        }

        protected override void OnHitWallHook()
        {
            base.OnHitWallHook();

            speedX = speedY = 0f;
        }

        protected override void OnHitFloorHook()
        {
            base.OnHitFloorHook();

            speedX = speedY = 0f;
        }

        protected override void OnHitCeilingHook()
        {
            base.OnHitCeilingHook();

            speedX = speedY = 0f;
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);
            api.PlayCommonSound(this, "Splat");

            Explosion.Create(api, Transform.Pos, Explosion.SmallDark);

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}