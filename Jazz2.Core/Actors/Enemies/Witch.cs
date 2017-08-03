using System.Collections.Generic;
using Duality;
using Jazz2.Game;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Witch : EnemyBase
    {
        private const float DefaultSpeed = -4f;

        private float attackTime;
        private bool playerHit;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            collisionFlags &= ~CollisionFlags.ApplyGravitation;

            SetHealthByDifficulty(30);
            scoreValue = 1000;

            RequestMetadata("Enemy/Witch");
            SetAnimation(AnimState.Idle);
        }

        protected override void OnUpdate()
        {
            OnUpdateHitbox();
            HandleBlinking();

            if (frozenTimeLeft > 0) {
                frozenTimeLeft -= Time.TimeMult;
                return;
            }

            MoveInstantly(new Vector2(speedX, speedY), MoveType.RelativeTime, true);

            if (playerHit) {
                if (attackTime > 0f) {
                    attackTime -= Time.TimeMult;
                } else {
                    base.OnPerish(null);
                }
                return;
            }

            if (attackTime > 0f) {
                attackTime -= Time.TimeMult;
            }

            Vector3 pos = Transform.Pos;
            Vector3 targetPos;

            List<Player> players = api.Players;
            for (int i = 0; i < players.Count; i++) {
                targetPos = players[i].Transform.Pos;
                // Fly above the player
                targetPos.Y -= 100f;

                Vector3 direction = (pos - targetPos);
                float length = direction.Length;

                if (attackTime <= 0f && length < 260f) {
                    attackTime = 450f;

                    PlaySound("MagicFire");

                    SetTransition(AnimState.TransitionAttack, true, delegate {
                        Vector3 bulletPos = Transform.Pos + new Vector3(24f * (isFacingLeft ? -1f : 1f), 0f, -2f);

                        MagicBullet bullet = new MagicBullet(this);
                        bullet.OnAttach(new ActorInstantiationDetails {
                            Api = api,
                            Pos = bulletPos
                        });
                        api.AddActor(bullet);

                        Explosion.Create(api, bulletPos, Explosion.TinyDark);
                    });
                } else if (length < 500f) {
                    direction.Normalize();
                    speedX = (direction.X * DefaultSpeed + speedX) * 0.5f;
                    speedY = (direction.Y * DefaultSpeed + speedY) * 0.5f;

                    isFacingLeft = (speedX < 0f);
                    RefreshFlipMode();
                    return;
                }
            }

            speedX = speedY = 0;
        }

        protected override bool OnPerish(ActorBase collider)
        {
            api.PlayCommonSound(this, "Splat");

            SetTransition(AnimState.TransitionDeath, false, delegate {
                base.OnPerish(collider);
            });

            CreateParticleDebris();

            return false;
        }

        public override bool OnTileDeactivate(int tx, int ty, int tileDistance)
        {
            return false;
        }

        private void OnPlayerHit()
        {
            playerHit = true;
            attackTime = 400f;

            speedX = (isFacingLeft ? -1f : 1f) * 9f;
            speedY = -0.8f;

            PlaySound("Laugh");
        }

        public class MagicBullet : EnemyBase
        {
            private Witch owner;
            private float time = 380f;

            public MagicBullet(Witch owner)
            {
                this.owner = owner;
            }

            public override void OnAttach(ActorInstantiationDetails details)
            {
                base.OnAttach(details);

                canHurtPlayer = false;
                canBeFrozen = false;
                collisionFlags = CollisionFlags.CollideWithOtherActors | CollisionFlags.SkipPerPixelCollisions;

                health = int.MaxValue;

                RequestMetadata("Enemy/Witch");

                SetAnimation((AnimState)1073741828);

                LightEmitter light = AddComponent<LightEmitter>();
                light.Intensity = 0.85f;
                light.RadiusNear = 0f;
                light.RadiusFar = 18f;
            }

            protected override void OnUpdate()
            {
                //base.OnUpdate();
                MoveInstantly(new Vector2(speedX, speedY), MoveType.RelativeTime, true);
                OnUpdateHitbox();

                if (time <= 0f) {
                    DecreaseHealth(int.MaxValue);
                } else {
                    time -= Time.TimeMult;

                    FollowNearestPlayer();
                }

                foreach (ActorBase collision in api.FindCollisionActors(this)) {
                    if (collision is Player) {
                        DecreaseHealth(int.MaxValue);
                        owner.OnPlayerHit();
                        // ToDo: Frog
                    }
                }
            }

            protected override void OnUpdateHitbox()
            {
                UpdateHitbox(10, 10);
            }

            public override void HandleCollision(ActorBase other)
            {
            }

            private void FollowNearestPlayer()
            {
                bool found = false;
                Vector3 pos = Transform.Pos;
                Vector3 targetPos = new Vector3(float.MaxValue, float.MaxValue, pos.Z);

                List<Player> players = api.Players;
                for (int i = 0; i < players.Count; i++) {
                    Vector3 newPos = players[i].Transform.Pos;
                    if ((pos - newPos).Length < (pos - targetPos).Length) {
                        targetPos = newPos;
                        found = true;
                    }
                }

                if (found) {
                    Vector3 diff = (targetPos - pos).Normalized;
                    Vector3 speed = (new Vector3(speedX, speedY, 0f) + diff * 0.8f).Normalized;
                    speedX = speed.X * 5f;
                    speedY = speed.Y * 5f;

                    Transform.Angle = MathF.Atan2(speedY, speedX);
                }
            }
        }
    }
}