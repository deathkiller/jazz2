using System.Collections.Generic;
using Duality;
using Duality.Audio;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Dragonfly : EnemyBase
    {
        private const int StateIdle = 0;
        private const int StateAttacking = 1;
        private const int StateBraking = 2;

        private int state = StateIdle;
        private float idleTime;
        private float attackCooldown = 60f;
        private Vector2 direction;

        private SoundInstance noise;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            collisionFlags &= ~CollisionFlags.ApplyGravitation;

            SetHealthByDifficulty(1);
            scoreValue = 200;

            RequestMetadata("Enemy/Dragonfly");
            SetAnimation(AnimState.Idle);

            isFacingLeft = MathF.Rnd.NextBool();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (frozenTimeLeft > 0) {
                return;
            }

            canJump = false;

            Vector3 pos = Transform.Pos;
            if (attackCooldown < 0f) {
                attackCooldown = 40f;

                Vector3 targetPos;

                List<Player> players = api.Players;
                for (int i = 0; i < players.Count; i++) {
                    targetPos = players[i].Transform.Pos;
                    direction = (targetPos.Xy - pos.Xy);
                    float length = direction.Length;
                    if (length < 320f && targetPos.Y < api.WaterLevel) {
                        direction.Normalize();

                        speedX = speedY = 0f;
                        isFacingLeft = (direction.X < 0f);
                        state = StateAttacking;

                        idleTime = MathF.Rnd.NextFloat(40f, 60f);
                        attackCooldown = MathF.Rnd.NextFloat(130f, 200f);

                        noise = PlaySound("Noise", 0.8f);
                        break;
                    }
                }
            } else {
                float timeMult = Time.TimeMult;

                if (state == StateAttacking) {
                    if (idleTime < 0f) {
                        state = StateBraking;

                        if (noise != null) {
                            noise.FadeOut(1f);
                            noise = null;
                        }
                    } else {
                        idleTime -= Time.TimeMult;

                        speedX += direction.X * 0.14f * timeMult;
                        speedY += direction.Y * 0.14f * timeMult;
                    }
                } else if (state == StateBraking) {
                    speedX *= 0.88f / timeMult;
                    speedY *= 0.88f / timeMult;

                    if (MathF.Abs(speedX) < 0.01f && MathF.Abs(speedY) < 0.01f) {
                        state = StateIdle;
                    }
                } else {
                    if (idleTime < 0f) {
                        float x = MathF.Rnd.NextFloat(-0.4f, 0.4f);
                        float y = MathF.Rnd.NextFloat(-2f, 2f);

                        speedX = (speedX + x) * 0.2f;
                        speedY = (speedY + y) * 0.2f;

                        idleTime = 20f;
                    } else {
                        idleTime -= timeMult;
                    }
                }

                // Can't fly into the water
                if (pos.Y > api.WaterLevel - 12f) {
                    speedY = -0.4f;
                    state = StateIdle;
                }

                attackCooldown -= timeMult;
            }
        }

        protected override void OnHitWallHook()
        {
            base.OnHitWallHook();

            speedX = speedY = 0f;

            if (noise != null) {
                noise.FadeOut(0.4f);
                noise = null;
            }
        }

        protected override void OnHitFloorHook()
        {
            base.OnHitFloorHook();

            speedX = speedY = 0f;

            if (noise != null) {
                noise.FadeOut(0.4f);
                noise = null;
            }
        }

        protected override void OnHitCeilingHook()
        {
            base.OnHitCeilingHook();

            speedX = speedY = 0f;

            if (noise != null) {
                noise.FadeOut(0.4f);
                noise = null;
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            if (noise != null) {
                noise.Stop();
                noise = null;
            }

            CreateDeathDebris(collider);
            api.PlayCommonSound(this, "Splat");

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}