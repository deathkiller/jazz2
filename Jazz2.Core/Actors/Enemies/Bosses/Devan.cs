using System.Collections.Generic;
using System.Threading.Tasks;
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

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            endText = details.Params[1];

            SetHealthByDifficulty(140 * 2);
            scoreValue = 10000;

            await RequestMetadataAsync("Boss/Devan");
            SetAnimation(AnimState.Idle);

            renderer.Active = false;

            isInvulnerable = true;
        }

        protected override void OnBossActivated()
        {
            state = StateWarpingIn;
            stateTime = 120f;
        }

        public override void OnFixedUpdate(float timeMult)
        {
            if (isDemon) {
                OnUpdateHitbox();
                HandleBlinking(timeMult);

                MoveInstantly(new Vector2(speedX * timeMult, speedY * timeMult), MoveType.Relative, true);
            } else {
                base.OnFixedUpdate(timeMult);
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
                            IsFacingLeft = (targetPos.X < pos.X);
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
                            IsFacingLeft ^= true;
                            speedX = (IsFacingLeft ? -4f : 4f);
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
                        attackTime -= timeMult;
                        FollowNearestPlayerDemon(timeMult);
                    }
                    break;
                }

                case StateDemonSpewingFireball: {
                    state = StateTransition;
                    SetTransition((AnimState)673, false, delegate {
                        PlaySound("SpitFireball");

                        Fireball fireball = new Fireball();
                        fireball.OnActivated(new ActorActivationDetails {
                            Api = api,
                            Pos = Transform.Pos + new Vector3(IsFacingLeft ? -26f : 26f, -14f, 0f),
                            Params = new[] { (ushort)(IsFacingLeft ? 1 : 0) }
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

            stateTime -= timeMult;
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

                IsFacingLeft = (targetPos.X < pos.X);

                speedX = (IsFacingLeft ? -4f : 4f);

                //PlaySound("RUN");
                SetAnimation(AnimState.Run);
            }
        }

        private void FollowNearestPlayerDemon(float timeMult)
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

                anglePhase += timeMult * 0.04f;

                Vector3 speed = ((targetPos - lastPos) / 70f + lastSpeed * 1.4f) / 2.4f;
                lastPos.X += speed.X;
                lastPos.Y += speed.Y;
                lastSpeed = speed;

                bool willFaceLeft = (speed.X < 0f);
                if (IsFacingLeft != willFaceLeft) {
                    SetTransition(AnimState.TransitionTurn, false, delegate {
                        IsFacingLeft = willFaceLeft;
                    });
                }

                Transform.Pos = lastPos + new Vector3(0f, MathF.Sin(anglePhase) * 30f, 0f);
            }
        }

        private void Shoot()
        {
            PlaySound(Transform.Pos, "Shoot");

            SetTransition((AnimState)16, false, delegate {
                Bullet bullet = new Bullet();
                bullet.OnActivated(new ActorActivationDetails {
                    Api = api,
                    Pos = Transform.Pos + new Vector3(IsFacingLeft ? -24f : 24f, 2f, 0f),
                    Params = new[] { (ushort)(IsFacingLeft ? 1 : 0) }
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
            protected override async Task OnActivatedAsync(ActorActivationDetails details)
            {
                IsFacingLeft = (details.Params[0] != 0);

                base.canCollideWithAmmo = false;
                base.isInvulnerable = true;
                base.collisionFlags &= ~CollisionFlags.ApplyGravitation;

                canBeFrozen = false;
                health = int.MaxValue;

                speedX = (IsFacingLeft ? -8f : 8f);

                await RequestMetadataAsync("Boss/Devan");
                SetAnimation((AnimState)668);

                LightEmitter light = AddComponent<LightEmitter>();
                light.Intensity = 0.8f;
                light.Brightness = 0.8f;
                light.RadiusNear = 0f;
                light.RadiusFar = 28f;
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

            public override void OnHandleCollision(ActorBase other)
            {
            }

            protected override void OnHitFloor()
            {
                DecreaseHealth(int.MaxValue);

                PlaySound(Transform.Pos, "WallPoof");
            }

            protected override void OnHitWall()
            {
                DecreaseHealth(int.MaxValue);

                PlaySound(Transform.Pos, "WallPoof");
            }

            protected override void OnHitCeiling()
            {
                DecreaseHealth(int.MaxValue);

                PlaySound(Transform.Pos, "WallPoof");
            }
        }

        private class Fireball : EnemyBase
        {
            protected override async Task OnActivatedAsync(ActorActivationDetails details)
            {
                IsFacingLeft = (details.Params[0] != 0);

                base.canBeFrozen = false;
                base.isInvulnerable = true;
                base.collisionFlags &= ~CollisionFlags.ApplyGravitation;

                health = int.MaxValue;

                speedX = (IsFacingLeft ? -5f : 5f);
                speedY = 5f;

                await RequestMetadataAsync("Boss/Devan");
                SetAnimation((AnimState)675);

                LightEmitter light = AddComponent<LightEmitter>();
                light.Intensity = 0.85f;
                light.Brightness = 0.4f;
                light.RadiusNear = 0f;
                light.RadiusFar = 30f;
            }

            protected override void OnUpdateHitbox()
            {
                UpdateHitbox(6, 6);
            }

            protected override bool OnPerish(ActorBase collider)
            {
                Explosion.Create(api, Transform.Pos, Explosion.SmallDark);

                PlaySound(Transform.Pos, "Flap");

                return base.OnPerish(collider);
            }

            public override void OnHandleCollision(ActorBase other)
            {
            }

            protected override void OnHitFloor()
            {
                DecreaseHealth(int.MaxValue);
            }

            protected override void OnHitWall()
            {
                DecreaseHealth(int.MaxValue);
            }

            protected override void OnHitCeiling()
            {
                DecreaseHealth(int.MaxValue);
            }
        }
    }
}