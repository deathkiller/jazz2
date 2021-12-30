﻿using System.Collections.Generic;
using System.Threading.Tasks;
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

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Enemy/Fish");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new Fish();
            actor.OnActivated(details);
            return actor;
        }

        private Fish()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            CollisionFlags &= ~CollisionFlags.ApplyGravitation;

            SetHealthByDifficulty(1);
            scoreValue = 100;

            await RequestMetadataAsync("Enemy/Fish");
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

            if (attackCooldown < 0f) {
                attackCooldown = 60f;

                Vector3 pos = Transform.Pos;
                Vector3 targetPos;

                List<Player> players = levelHandler.Players;
                for (int i = 0; i < players.Count; i++) {
                    targetPos = players[i].Transform.Pos;
                    direction = (targetPos.Xy - pos.Xy);
                    float length = direction.Length;
                    if (length < 320f) {
                        direction.Normalize();

                        speedX = 0f;
                        speedY = 0f;
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

        protected override void OnHitWall()
        {
            base.OnHitWall();

            speedX = 0f;
            speedY = 0f;
        }

        protected override void OnHitFloor()
        {
            base.OnHitFloor();

            speedX = 0f;
            speedY = 0f;
        }

        protected override void OnHitCeiling()
        {
            base.OnHitCeiling();

            speedX = 0f;
            speedY = 0f;
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);
            levelHandler.PlayCommonSound("Splat", Transform.Pos);

            Explosion.Create(levelHandler, Transform.Pos, Explosion.SmallDark);

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}