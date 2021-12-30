﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Components;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Witch : EnemyBase
    {
        private const float DefaultSpeed = -4f;

        private float attackTime;
        private bool playerHit;

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Enemy/Witch");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new Witch();
            actor.OnActivated(details);
            return actor;
        }

        private Witch()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            CollisionFlags &= ~CollisionFlags.ApplyGravitation;

            SetHealthByDifficulty(30);
            scoreValue = 1000;

            await RequestMetadataAsync("Enemy/Witch");
            SetAnimation(AnimState.Idle);

            PreloadMetadata("Interactive/PlayerFrog");
        }

        public override void OnFixedUpdate(float timeMult)
        {
            OnUpdateHitbox();
            HandleBlinking(timeMult);

            if (frozenTimeLeft > 0) {
                frozenTimeLeft -= timeMult;
                return;
            }

            MoveInstantly(new Vector2(speedX * timeMult, speedY * timeMult), MoveType.Relative, true);

            if (playerHit) {
                if (attackTime > 0f) {
                    attackTime -= timeMult;
                } else {
                    base.OnPerish(null);
                }
                return;
            }

            if (attackTime > 0f) {
                attackTime -= timeMult;
            }

            Vector3 pos = Transform.Pos;
            Vector3 targetPos;

            List<Player> players = levelHandler.Players;
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
                        Vector3 bulletPos = Transform.Pos + new Vector3(24f * (IsFacingLeft ? -1f : 1f), 0f, -2f);

                        MagicBullet bullet = new MagicBullet(this);
                        bullet.OnActivated(new ActorActivationDetails {
                            LevelHandler = levelHandler,
                            Pos = bulletPos
                        });
                        levelHandler.AddActor(bullet);

                        Explosion.Create(levelHandler, bulletPos, Explosion.TinyDark);
                    });
                } else if (length < 500f) {
                    direction.Normalize();
                    speedX = (direction.X * DefaultSpeed + speedX) * 0.5f;
                    speedY = (direction.Y * DefaultSpeed + speedY) * 0.5f;

                    IsFacingLeft = (speedX < 0f);
                    return;
                }
            }

            speedX = 0f;
            speedY = 0f;
        }

        protected override bool OnPerish(ActorBase collider)
        {
            levelHandler.PlayCommonSound("Splat", Transform.Pos);

            SetTransition(AnimState.TransitionDeath, false, delegate {
                base.OnPerish(collider);
            });

            CreateParticleDebris();

            return false;
        }

        public override bool OnTileDeactivate(int tx1, int ty1, int tx2, int ty2)
        {
            return false;
        }

        private void OnPlayerHit()
        {
            playerHit = true;
            attackTime = 400f;

            speedX = (IsFacingLeft ? -1f : 1f) * 9f;
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

                base.canCollideWithAmmo = false;
            }

            protected override async Task OnActivatedAsync(ActorActivationDetails details)
            {
                base.canBeFrozen = false;
                base.canCollideWithAmmo = false;
                base.canHurtPlayer = false;
                base.isInvulnerable = true;
                base.CollisionFlags = CollisionFlags.CollideWithOtherActors | CollisionFlags.SkipPerPixelCollisions;

                health = int.MaxValue;

                await RequestMetadataAsync("Enemy/Witch");

                SetAnimation((AnimState)1073741828);

                LightEmitter light = AddComponent<LightEmitter>();
                light.Intensity = 0.85f;
                light.RadiusNear = 0f;
                light.RadiusFar = 18f;
            }

            public override void OnFixedUpdate(float timeMult)
            {
                //base.OnFixedUpdate(timeMult);
                MoveInstantly(new Vector2(speedX * timeMult, speedY * timeMult), MoveType.Relative, true);
                OnUpdateHitbox();

                if (time <= 0f) {
                    DecreaseHealth(int.MaxValue);
                } else {
                    time -= timeMult;

                    FollowNearestPlayer();
                }
            }

            protected override void OnUpdateHitbox()
            {
                UpdateHitbox(10, 10);
            }

            public override void OnHandleCollision(ActorBase other)
            {
                switch (other) {
                    case Player player: {
                        DecreaseHealth(int.MaxValue);
                        owner.OnPlayerHit();

                        player.MorphTo(PlayerType.Frog);
                        break;
                    }
                }
            }

            private void FollowNearestPlayer()
            {
                bool found = false;
                Vector3 pos = Transform.Pos;
                Vector3 targetPos = new Vector3(float.MaxValue, float.MaxValue, pos.Z);

                List<Player> players = levelHandler.Players;
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