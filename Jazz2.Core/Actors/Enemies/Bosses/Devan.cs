using System.Collections.Generic;
using Duality;
using Jazz2.Actors.Enemies;
using Jazz2.Game;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Bosses
{
    public class Devan : BossBase
    {
        private const int StateTransition = -1;
        private const int StateWaiting = 0;
        private const int StateWarpingIn = 1;
        private const int StateIdling = 2;
        private const int StateRunning1 = 3;
        private const int StateRunning2 = 4;
        private const int StateDemonFlying = 5;
        private const int StateDemonSpewingFireball = 6;
        private const int StateFalling = 7;

        private int state = StateWaiting;
        private float stateTime, attackTime = 90f;
        private int shots;
        private bool isDemon, isDead;

        private Vector3 lastPos, targetPos, lastSpeed;
        private float anglePhase;

        private ushort endText;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            endText = details.Params[1];

            SetHealthByDifficulty(140 * 2);
            scoreValue = 10000;

            RequestMetadata("Boss/Devan");
            SetAnimation(AnimState.Idle);

            renderer.Active = false;

            isInvulnerable = true;
        }

        public override void OnBossActivated()
        {
            state = StateWarpingIn;
            stateTime = 120f;
        }

        protected override void OnUpdate()
        {
            if (isDemon) {
                OnUpdateHitbox();
                HandleBlinking();

                MoveInstantly(new Vector2(speedX, speedY), MoveType.RelativeTime, true);
            } else {
                base.OnUpdate();
            }
            
            if (frozenTimeLeft > 0) {
                return;
            }

            switch (state) {
                case StateWarpingIn: {
                    if (stateTime <= 0f) {
                        bool found = false;
                        Vector3 pos = Transform.Pos;
                        Vector3 targetPos = new Vector3(float.MaxValue, float.MaxValue, 0f);

                        List<Player> players = api.Players;
                        for (int i = 0; i < players.Count; i++) {
                            Vector3 newPos = players[i].Transform.Pos;
                            if ((pos - newPos).Length < (pos - targetPos).Length) {
                                targetPos = newPos;
                                found = true;
                            }
                        }

                        if (found) {
                            isFacingLeft = (targetPos.X < pos.X);
                            RefreshFlipMode();
                        }

                        renderer.Active = true;

                        state = StateTransition;
                        SetTransition(AnimState.TransitionWarpIn, false, delegate {
                            state = StateIdling;
                            stateTime = 80f;

                            isInvulnerable = false;
                        });
                    }

                    break;
                }

                case StateIdling: {
                    if (stateTime <= 0f) {
                        FollowNearestPlayer(StateRunning1, MathF.Rnd.NextFloat(60, 120));
                    }

                    break;
                }

                case StateRunning1: {
                    if (stateTime <= 0f) {
                        if (health < maxHealth / 2) {
                            isDemon = true;
                            speedX = 0f;
                            speedY = 0f;

                            isInvulnerable = true;
                            canBeFrozen = false;

                            state = StateTransition;
                            // DEMON_FLY
                            SetAnimation((AnimState)669);
                            // DEMON_TRANSFORM_START
                            SetTransition((AnimState)670, false, delegate {
                                collisionFlags &= ~CollisionFlags.ApplyGravitation;
                                isInvulnerable = false;

                                state = StateDemonFlying;

                                lastPos = Transform.Pos;
                                targetPos = lastPos + new Vector3(0f, -200f, 0f);
                            });
                        } else {
                            if (MathF.Rnd.NextFloat() < 0.5f) {
                                FollowNearestPlayer(
                                    StateRunning1,
                                    MathF.Rnd.NextFloat(60, 120));
                            } else {
                                FollowNearestPlayer(
                                    StateRunning2,
                                    MathF.Rnd.NextFloat(10, 30));
                            }
                        }
                            
                    } else {
                        if (!CanMoveToPosition(speedX, 0)) {
                            isFacingLeft ^= true;
                            speedX = (isFacingLeft ? -4f : 4f);
                        }
                    }
                    break;
                }

                case StateRunning2: {
                    if (stateTime <= 0f) {
                        speedX = 0f;

                        state = StateTransition;
                        SetTransition(AnimState.TransitionRunToIdle, false, delegate {
                            SetTransition((AnimState)15, false, delegate {
                                shots = MathF.Rnd.Next(1, 8);
                                Shoot();
                            });
                        });
                    }
                    break;
                }

                case StateDemonFlying: {
                    if (attackTime <= 0f) {
                        state = StateDemonSpewingFireball;
                    } else {
                        attackTime -= Time.TimeMult;
                        FollowNearestPlayerDemon();
                    }
                    break;
                }

                case StateDemonSpewingFireball: {
                    state = StateTransition;
                    SetTransition((AnimState)673, false, delegate {
                        PlaySound("SpitFireball");

                        Fireball fireball = new Fireball();
                        fireball.OnAttach(new ActorInstantiationDetails {
                            Api = api,
                            Pos = Transform.Pos + new Vector3(isFacingLeft ? -26f : 26f, -14f, 0f),
                            Params = new[] { (ushort)(isFacingLeft ? 1 : 0) }
                        });
                        api.AddActor(fireball);

                        SetTransition((AnimState)674, false, delegate {
                            state = StateDemonFlying;

                            attackTime = MathF.Rnd.NextFloat(100f, 240f);
                        });
                    });
                    break;
                }

                case StateFalling: {
                        if (canJump) {
                            state = StateTransition;
                            // DISORIENTED_START
                            SetTransition((AnimState)666, false, delegate {
                                // DISORIENTED
                                SetTransition((AnimState)667, false, delegate {
                                    // DISORIENTED
                                    SetTransition((AnimState)667, false, delegate {
                                        // DISORIENTED_WARP_OUT
                                        SetTransition((AnimState)6670, false, delegate {
                                            base.OnPerish(null);
                                        });
                                    });
                                });
                            });
                        }
                    break;
                }
            }

            stateTime -= Time.TimeMult;
        }

        protected override bool OnPerish(ActorBase collider)
        {
            if (isDead) {
                return false;
            }

            api.BroadcastLevelText(endText);

            isDead = true;

            speedX = 0f;
            speedY = 0f;

            collisionFlags &= ~CollisionFlags.ApplyGravitation;

            state = StateTransition;
            SetTransition((AnimState)671, false, delegate {
                collisionFlags |= CollisionFlags.ApplyGravitation;

                isDemon = false;
                state = StateFalling;
                SetAnimation(AnimState.Freefall);
            });

            return false;
        }

        private void FollowNearestPlayer(int newState, float time)
        {
            bool found = false;
            Vector3 pos = Transform.Pos;
            Vector3 targetPos = new Vector3(float.MaxValue, float.MaxValue, 0f);

            List<Player> players = api.Players;
            for (int i = 0; i < players.Count; i++) {
                Vector3 newPos = players[i].Transform.Pos;
                if ((pos - newPos).Length < (pos - targetPos).Length) {
                    targetPos = newPos;
                    found = true;
                }
            }

            if (found) {
                state = newState;
                stateTime = time;

                isFacingLeft = (targetPos.X < pos.X);

                speedX = (isFacingLeft ? -4f : 4f);

                //PlaySound("RUN");
                SetAnimation(AnimState.Run);
            }
        }

        private void FollowNearestPlayerDemon()
        {
            bool found = false;
            Vector3 foundPos = new Vector3(float.MaxValue, float.MaxValue, lastPos.Z);

            List<Player> players = api.Players;
            for (int i = 0; i < players.Count; i++) {
                Vector3 newPos = players[i].Transform.Pos;
                if ((lastPos - newPos).Length < (lastPos - foundPos).Length) {
                    foundPos = newPos;
                    found = true;
                }
            }

            if (found) {
                Vector3 diff = (foundPos - lastPos);

                targetPos = foundPos;
                targetPos.Y -= 70f;

                anglePhase += Time.TimeMult * 0.04f;

                Vector3 speed = ((targetPos - lastPos) / 70f + lastSpeed * 1.4f) / 2.4f;
                lastPos.X += speed.X;
                lastPos.Y += speed.Y;
                lastSpeed = speed;

                bool willFaceLeft = (speed.X < 0f);
                if (isFacingLeft != willFaceLeft) {
                    SetTransition(AnimState.TransitionTurn, false, delegate {
                        isFacingLeft = willFaceLeft;
                        RefreshFlipMode();
                    });
                }

                Transform.Pos = lastPos + new Vector3(0f, MathF.Sin(anglePhase) * 30f, 0f);
            }
        }

        private void Shoot()
        {
            PlaySound("Shoot");

            SetTransition((AnimState)16, false, delegate {
                Bullet bullet = new Bullet();
                bullet.OnAttach(new ActorInstantiationDetails {
                    Api = api,
                    Pos = Transform.Pos + new Vector3(isFacingLeft ? -24f : 24f, 2f, 0f),
                    Params = new[] { (ushort)(isFacingLeft ? 1 : 0) }
                });
                api.AddActor(bullet);

                shots--;

                SetTransition((AnimState)17, false, delegate {
                    if (shots > 0) {
                        Shoot();
                    } else {
                        Run();
                    }
                });
            });
        }

        private void Run()
        {
            SetTransition((AnimState)18, false, delegate {
                FollowNearestPlayer(
                    StateRunning1,
                    MathF.Rnd.NextFloat(60, 150));
            });
        }

        private class Bullet : EnemyBase
        {
            public override void OnAttach(ActorInstantiationDetails details)
            {
                base.OnAttach(details);

                isFacingLeft = (details.Params[0] != 0);

                collisionFlags &= ~CollisionFlags.ApplyGravitation;

                canBeFrozen = false;
                health = int.MaxValue;

                speedX = (isFacingLeft ? -8f : 8f);

                RequestMetadata("Boss/Devan");
                SetAnimation((AnimState)668);

                OnUpdateHitbox();

                LightEmitter light = AddComponent<LightEmitter>();
                light.Intensity = 0.8f;
                light.Brightness = 0.8f;
                light.RadiusNear = 0f;
                light.RadiusFar = 28f;
            }

            protected override void OnUpdate()
            {
                base.OnUpdate();
            }

            protected override void OnUpdateHitbox()
            {
                UpdateHitbox(6, 6);
            }

            protected override bool OnPerish(ActorBase collider)
            {
                Explosion.Create(api, Transform.Pos, Explosion.Small);

                return base.OnPerish(collider);
            }

            public override void HandleCollision(ActorBase other)
            {
            }

            protected override void OnHitFloorHook()
            {
                DecreaseHealth(int.MaxValue);

                PlaySound("WallPoof");
            }

            protected override void OnHitWallHook()
            {
                DecreaseHealth(int.MaxValue);

                PlaySound("WallPoof");
            }

            protected override void OnHitCeilingHook()
            {
                DecreaseHealth(int.MaxValue);

                PlaySound("WallPoof");
            }
        }

        private class Fireball : EnemyBase
        {
            public override void OnAttach(ActorInstantiationDetails details)
            {
                base.OnAttach(details);

                isFacingLeft = (details.Params[0] != 0);

                collisionFlags &= ~CollisionFlags.ApplyGravitation;

                canBeFrozen = false;
                health = int.MaxValue;

                speedX = (isFacingLeft ? -5f : 5f);
                speedY = 5f;

                RequestMetadata("Boss/Devan");
                SetAnimation((AnimState)675);

                OnUpdateHitbox();

                LightEmitter light = AddComponent<LightEmitter>();
                light.Intensity = 0.85f;
                light.Brightness = 0.4f;
                light.RadiusNear = 0f;
                light.RadiusFar = 30f;
            }

            protected override void OnUpdate()
            {
                base.OnUpdate();
            }

            protected override void OnUpdateHitbox()
            {
                UpdateHitbox(6, 6);
            }

            protected override bool OnPerish(ActorBase collider)
            {
                Explosion.Create(api, Transform.Pos, Explosion.SmallDark);

                PlaySound("Flap");

                return base.OnPerish(collider);
            }

            public override void HandleCollision(ActorBase other)
            {
            }

            protected override void OnHitFloorHook()
            {
                DecreaseHealth(int.MaxValue);
            }

            protected override void OnHitWallHook()
            {
                DecreaseHealth(int.MaxValue);
            }

            protected override void OnHitCeilingHook()
            {
                DecreaseHealth(int.MaxValue);
            }
        }
    }
}