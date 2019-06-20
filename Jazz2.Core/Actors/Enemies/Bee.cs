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
        private bool attacking;

        private SoundInstance noise;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            collisionFlags = CollisionFlags.CollideWithOtherActors;

            SetHealthByDifficulty(1);
            scoreValue = 200;

            originPos = lastPos = targetPos = details.Pos;

            await RequestMetadataAsync("Enemy/Bee");
            SetAnimation(AnimState.Idle);
        }

        protected override void OnFixedUpdate(float timeMult)
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

                    if (noise != null) {
                        noise.FadeOut(1f);
                        noise = null;
                    }
                } else {
                    AttackNearestPlayer();
                }
            }

            anglePhase += timeMult * 0.05f;

            Vector3 speed = ((targetPos - lastPos) / 30f + lastSpeed * 1.4f) / 2.4f;
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
            api.PlayCommonSound(this, "Splat");

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }

        private void AttackNearestPlayer()
        {
            bool found = false;
            targetPos = new Vector3(float.MaxValue, float.MaxValue, lastPos.Z);

            List<Player> players = api.Players;
            for (int i = 0; i < players.Count; i++) {
                Vector3 newPos = players[i].Transform.Pos;
                if ((lastPos - newPos).Length < (lastPos - targetPos).Length) {
                    targetPos = newPos;
                    found = true;
                }
            }

            // Can't fly into the water
            if (targetPos.Y > api.WaterLevel - 12f) {
                targetPos.Y = api.WaterLevel - 12f;
            }

            Vector3 diff = (targetPos - lastPos);
            if (found && diff.Length <= 250f) {
                attackTime = 90f;
                attacking = true;

                noise = PlaySound("Noise", 0.5f);
            } else {
                targetPos = originPos;
            }
        }
    }
}