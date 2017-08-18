using System.Collections.Generic;
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

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            originPos = Transform.Pos;

            collisionFlags = CollisionFlags.CollideWithOtherActors;

            SetHealthByDifficulty(1);
            scoreValue = 100;

            RequestMetadata("Enemy/Bat");
            SetAnimation(AnimState.Idle);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (frozenTimeLeft > 0) {
                return;
            }

            canJump = false;

            Vector3 pos = Transform.Pos;
            Vector3 targetPos;

            List<Player> players = api.Players;
            for (int i = 0; i < players.Count; i++) {
                targetPos = players[i].Transform.Pos;
                float visionDistance = (currentAnimationState == AnimState.Idle ? /*96f*/120f : 320f);
                if ((originPos - targetPos).Length < visionDistance) {
                    goto PLAYER_IS_CLOSE;
                }
            }

            if (attacking && currentTransitionState == AnimState.Idle) {
                Vector3 direction = (pos - originPos);
                float length = direction.Length;
                if (length < 2f) {
                    attacking = false;
                    Transform.Pos = originPos;
                    speedX = speedY = 0;
                    SetAnimation(AnimState.Idle);
                    SetTransition((AnimState)1073741826, false);
                } else {
                    direction.Normalize();
                    speedX = direction.X * DefaultSpeed;
                    speedY = direction.Y * DefaultSpeed;
                }
            }

            return;

        PLAYER_IS_CLOSE:
            if (attacking) {
                Vector3 direction = (pos - targetPos);
                direction.Normalize();
                speedX = direction.X * DefaultSpeed;
                speedY = direction.Y * DefaultSpeed;

                if (noiseCooldown > 0f) {
                    noiseCooldown -= Time.TimeMult;
                } else {
                    noiseCooldown = 60f;
                    PlaySound("Noise");
                }
            } else {
                if (currentTransitionState != AnimState.Idle)
                    return;

                speedX = speedY = 0;

                SetAnimation(AnimState.Walk);

                SetTransition((AnimState)1073741824, false, delegate {
                    attacking = true;

                    SetTransition((AnimState)1073741825, false);
                });
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);
            api.PlayCommonSound(this, "Splat");

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}