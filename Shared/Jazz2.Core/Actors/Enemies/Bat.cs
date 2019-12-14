using System.Collections.Generic;
using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Bat : EnemyBase
    {
        private float DefaultSpeed = -1f;

        private Vector3 originPos;
        private bool attacking;
        private float noiseCooldown;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            originPos = Transform.Pos;

            CollisionFlags = CollisionFlags.CollideWithOtherActors;

            SetHealthByDifficulty(1);
            scoreValue = 100;

            await RequestMetadataAsync("Enemy/Bat");
            SetAnimation(AnimState.Idle);
        }

        public override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            if (frozenTimeLeft > 0) {
                return;
            }

            canJump = false;

            Vector3 targetPos;
            if (FindNearestPlayer(out targetPos)) {
                if (attacking) {
                    // Can't fly into the water
                    if (targetPos.Y > levelHandler.WaterLevel - 20f) {
                        targetPos.Y = levelHandler.WaterLevel - 20f;
                    }

                    Vector3 direction = (Transform.Pos - targetPos);
                    direction.Normalize();
                    speedX = direction.X * DefaultSpeed;
                    speedY = direction.Y * DefaultSpeed;

                    if (noiseCooldown > 0f) {
                        noiseCooldown -= timeMult;
                    } else {
                        noiseCooldown = 60f;
                        PlaySound("Noise");
                    }
                } else {
                    if (currentTransitionState != AnimState.Idle) {
                        return;
                    }

                    speedX = 0;
                    speedY = 0;

                    SetAnimation(AnimState.Walk);

                    SetTransition((AnimState)1073741824, false, delegate {
                        attacking = true;

                        SetTransition((AnimState)1073741825, false);
                    });
                }
            } else {
                if (attacking && currentTransitionState == AnimState.Idle) {
                    Vector3 direction = (Transform.Pos - originPos);
                    float length = direction.Length;
                    if (length < 2f) {
                        attacking = false;
                        Transform.Pos = originPos;
                        speedX = 0;
                        speedY = 0;
                        SetAnimation(AnimState.Idle);
                        SetTransition((AnimState)1073741826, false);
                    } else {
                        direction.Normalize();
                        speedX = direction.X * DefaultSpeed;
                        speedY = direction.Y * DefaultSpeed;
                    }
                }
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);
            levelHandler.PlayCommonSound("Splat", Transform.Pos);

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }

        private bool FindNearestPlayer(out Vector3 targetPos)
        {
            const float VisionDistanceIdle = 120f;
            const float VisionDistanceAttacking = 320f;

            List<Player> players = levelHandler.Players;
            for (int i = 0; i < players.Count; i++) {
                targetPos = players[i].Transform.Pos;
                float visionDistance = (currentAnimationState == AnimState.Idle ? VisionDistanceIdle : VisionDistanceAttacking);
                if ((originPos - targetPos).Length < visionDistance) {
                    return true;
                }
            }

            targetPos = Vector3.Zero;
            return false;
        }
    }
}