using System.Collections.Generic;
using System.Threading.Tasks;
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

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            collisionFlags &= ~CollisionFlags.ApplyGravitation;

            SetHealthByDifficulty(1);
            scoreValue = 200;

            await RequestMetadataAsync("Enemy/Dragonfly");
            SetAnimation(AnimState.Idle);

            IsFacingLeft = MathF.Rnd.NextBool();
        }

        public override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            if (frozenTimeLeft > 0) {
                return;
            }

            canJump = false;

            Vector3 pos = Transform.Pos;
            if (attackCooldown < 0f) {
                attackCooldown = 40f;

                Vector3 targetPos;

                List<Player> players = levelHandler.Players;
                for (int i = 0; i < players.Count; i++) {
                    targetPos = players[i].Transform.Pos;
                    direction = (targetPos.Xy - pos.Xy);
                    float length = direction.Length;
                    if (length < 320f && targetPos.Y < levelHandler.WaterLevel) {
                        direction.Normalize();

                        speedX = 0f;
                        speedY = 0f;
                        IsFacingLeft = (direction.X < 0f);
                        state = StateAttacking;

                        idleTime = MathF.Rnd.NextFloat(40f, 60f);
                        attackCooldown = MathF.Rnd.NextFloat(130f, 200f);

                        noise = PlaySound("Noise", 0.8f);
                        break;
                    }
                }
            } else {
                if (state == StateAttacking) {
                    if (idleTime < 0f) {
                        state = StateBraking;

                        if (noise != null) {
                            noise.FadeOut(1f);
                            noise = null;
                        }
                    } else {
                        idleTime -= timeMult;

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
                if (pos.Y > levelHandler.WaterLevel - 12f) {
                    speedY = -0.4f;
                    state = StateIdle;

                    if (noise != null) {
                        noise.FadeOut(0.4f);
                        noise = null;
                    }
                }

                attackCooldown -= timeMult;
            }
        }

        protected override void OnHitWall()
        {
            base.OnHitWall();

            speedX = 0f;
            speedY = 0f;

            if (noise != null) {
                noise.FadeOut(0.4f);
                noise = null;
            }
        }

        protected override void OnHitFloor()
        {
            base.OnHitFloor();

            speedX = 0f;
            speedY = 0f;

            if (noise != null) {
                noise.FadeOut(0.4f);
                noise = null;
            }
        }

        protected override void OnHitCeiling()
        {
            base.OnHitCeiling();

            speedX = 0f;
            speedY = 0f;

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
            levelHandler.PlayCommonSound("Splat", Transform.Pos);

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}