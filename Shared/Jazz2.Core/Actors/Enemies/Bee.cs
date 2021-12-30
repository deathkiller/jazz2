using System.Collections.Generic;
using System.Threading.Tasks;
using Duality;
using Duality.Audio;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Bee : EnemyBase
    {
        private Vector3 originPos, lastPos, targetPos, lastSpeed;
        private float anglePhase;
        private float attackTime = 80f;
        private bool attacking, returning;

        private SoundInstance noise;

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Enemy/Bee");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new Bee();
            actor.OnActivated(details);
            return actor;
        }

        private Bee()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            CollisionFlags = CollisionFlags.CollideWithOtherActors;

            SetHealthByDifficulty(1);
            scoreValue = 200;

            originPos = lastPos = targetPos = details.Pos;

            await RequestMetadataAsync("Enemy/Bee");
            SetAnimation(AnimState.Idle);
        }

        public override void OnFixedUpdate(float timeMult)
        {
            OnUpdateHitbox();
            HandleBlinking(timeMult);

            if (frozenTimeLeft > 0) {
                frozenTimeLeft -= timeMult;
                return;
            }

            if (attackTime > 0f) {
                attackTime -= timeMult;
            } else {
                if (attacking) {
                    targetPos = originPos;

                    attackTime = 90f;
                    attacking = false;
                    returning = true;
                } else {
                    if (noise != null) {
                        noise.FadeOut(1f);
                        noise = null;
                    }

                    AttackNearestPlayer();
                }
            }

            anglePhase += timeMult * 0.05f;

            Vector3 speed = ((targetPos - lastPos) * (returning ? 0.03f : 0.006f) + lastSpeed * 1.4f) / 2.4f;
            lastPos.X += speed.X;
            lastPos.Y += speed.Y;
            lastSpeed = speed;

            bool willFaceLeft = (speed.X < 0f);
            if (IsFacingLeft != willFaceLeft) {
                SetTransition(AnimState.TransitionTurn, false, delegate {
                    IsFacingLeft = willFaceLeft;
                });
            }

            Transform.Pos = lastPos + new Vector3(MathF.Cos(anglePhase) * 16f, MathF.Sin(anglePhase) * -16f, 0f);
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

        private void AttackNearestPlayer()
        {
            bool found = false;
            targetPos = new Vector3(float.MaxValue, float.MaxValue, lastPos.Z);

            List<Player> players = levelHandler.Players;
            for (int i = 0; i < players.Count; i++) {
                Vector3 newPos = players[i].Transform.Pos;
                if ((lastPos - newPos).Length < (lastPos - targetPos).Length) {
                    targetPos = newPos;
                    found = true;
                }
            }

            Vector3 diff = (targetPos - lastPos);
            if (found && diff.Length <= 280f) {
                targetPos.X += (targetPos.X - originPos.X) * 1.8f;
                targetPos.Y += (targetPos.Y - originPos.Y) * 1.8f;

                // Can't fly into the water
                if (targetPos.Y > levelHandler.WaterLevel - 12f) {
                    targetPos.Y = levelHandler.WaterLevel - 12f;
                }

                attackTime = 110f;
                attacking = true;
                returning = false;

                if (noise == null) {
                    noise = PlaySound("Noise", 0.5f, 2f);
                    if (noise != null) {
                        noise.Flags |= SoundInstanceFlags.Looped;
                    }
                }
            } else {
                targetPos = originPos;
                returning = true;
            }
        }
    }
}