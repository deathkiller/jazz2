using System;
using Duality;
using Jazz2.Actors.Enemies;
using Jazz2.Game.Collisions;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;
using MathF = Duality.MathF;

namespace Jazz2.Actors.Bosses
{
    public class Queen : BossBase
    {
        private const int StateTransition = -1;
        private const int StateWaiting = 0;
        private const int StateIdleToScream = 1;
        private const int StateIdleToStomp = 2;
        private const int StateIdleToBackstep = 3;
        private const int StateDead = 4;
        private const int StateScreaming = 5;

        private int state = StateWaiting;
        private float stateTime;
        private int lastHealth;
        private bool queuedBackstep;
        private float brickStartRangeX;
        private InvisibleBlock block;

        private float stepSize;

        private ushort endText;

        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

            endText = details.Params[1];

            canHurtPlayer = false;
            isInvulnerable = true;
            canBeFrozen = false;
            health = int.MaxValue;
            maxHealth = int.MaxValue;
            scoreValue = 0;

            lastHealth = health;

            collisionFlags |= CollisionFlags.IsSolidObject | CollisionFlags.SkipPerPixelCollisions;

            stepSize = 0.3f;
            switch (api.Difficulty) {
                case GameDifficulty.Easy: stepSize *= 1.3f; break;
                case GameDifficulty.Hard: stepSize *= 0.7f; break;
            }

            RequestMetadata("Boss/Queen");
            SetAnimation(AnimState.Idle);

            // Invisible block above the queen
            block = new InvisibleBlock();
            block.OnActivated(new ActorActivationDetails {
                Api = api
            });
            api.AddActor(block);
        }

        protected override void OnDeactivated(Component.ShutdownContext context)
        {
            if (block != null) {
                api.RemoveActor(block);
                block = null;
            }

            base.OnDeactivated(context);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (block != null) {
                block.UpdateBlock(Transform.Pos);
            }

            switch (state) {
                case StateWaiting: {
                    // Waiting for player to enter the arena
                    Vector3 pos = Transform.Pos;
                    api.FindCollisionActorsByAABB(this, new AABB(pos.X - 300, pos.Y - 120, pos.X + 60, pos.Y + 120), actor => {
                        if (actor is Player) {
                            state = StateIdleToScream;
                            stateTime = 260f;

                            brickStartRangeX = pos.X - 180f;
                            return false;
                        }
                        return true;
                    });
                    break;
                }

                case StateIdleToScream: {
                    // Scream towards the player
                    if (stateTime <= 0f) {
                        lastHealth = health;
                        isInvulnerable = false;

                        state = StateScreaming;
                        PlaySound("Scream");
                        SetTransition((AnimState)1073741824, false, delegate {
                            state = (MathF.Rnd.NextFloat() < 0.8f ? StateIdleToStomp : StateIdleToBackstep);
                            stateTime = MathF.Rnd.NextFloat(65, 85);

                            isInvulnerable = true;

                            if (lastHealth - health >= 2) {
                                queuedBackstep = true;
                            }
                        });
                    }
                    break;
                }

                case StateIdleToStomp: {
                    if (stateTime <= 0f) {
                        state = StateTransition;
                        SetTransition((AnimState)1073741825, false, delegate {
                            PlaySound("Stomp");

                            SetTransition((AnimState)1073741830, false, delegate {
                                state = StateIdleToBackstep;
                                stateTime = MathF.Rnd.NextFloat(70, 80);

                                Vector3 pos = Transform.Pos;

                                Brick brick = new Brick();
                                brick.OnActivated(new ActorActivationDetails {
                                    Api = api,
                                    Pos = new Vector3(brickStartRangeX + MathF.Rnd.NextFloat(pos.X - brickStartRangeX - 50), pos.Y - 200f, pos.Z + 20f)
                                });
                                api.AddActor(brick);

                                api.ShakeCameraView(20f);
                            });
                        });
                    }
                    break;
                }

                case StateIdleToBackstep: {
                    if (stateTime <= 0f) {
                        if (queuedBackstep) {
                            queuedBackstep = false;

                            // 2 hits by player while screaming, step backwards
                            speedX = stepSize;

                            state = StateTransition;
                            SetTransition((AnimState)1073741826, false, delegate {
                                speedX = 0f;

                                // Check it it's on the ledge
                                Vector3 pos = Transform.Pos;
                                AABB aabb1 = new AABB(pos.X - 10, pos.Y + 24, pos.X - 6, pos.Y + 28);
                                AABB aabb2 = new AABB(pos.X + 6, pos.Y + 24, pos.X + 10, pos.Y + 28);
                                if (!api.IsPositionEmpty(this, ref aabb1, true) && api.IsPositionEmpty(this, ref aabb2, true)) {
                                    lastHealth = health;
                                    isInvulnerable = false;

                                    // It's on the ledge
                                    state = StateTransition;
                                    SetTransition((AnimState)1073741827, false, delegate {
                                        isInvulnerable = true;

                                        // 1 hits by player
                                        if (lastHealth - health >= 1) {
                                            // Fall off the ledge
                                            speedX = 1.8f;

                                            SetAnimation(AnimState.Fall);
                                        } else {
                                            // Recover, step forward
                                            SetTransition((AnimState)1073741828, false, delegate {
                                                speedX = stepSize * -1.3f;

                                                SetTransition((AnimState)1073741826, false, delegate {
                                                    speedX = 0f;

                                                    state = StateIdleToScream;
                                                    stateTime = MathF.Rnd.NextFloat(150, 180);
                                                });
                                            });
                                        }
                                            
                                    });
                                } else {
                                    state = StateIdleToScream;
                                    stateTime = MathF.Rnd.NextFloat(160, 200);
                                }
                            });
                        } else {
                            state = StateIdleToScream;
                            stateTime = MathF.Rnd.NextFloat(150, 180);
                        }
                    }
                    break;
                }

                case StateDead: {
                    // Thrown away by spring
                    CheckDestructibleTiles();

                    if (stateTime <= 0f) {
                        DecreaseHealth(int.MaxValue);
                    }
                    break;
                }

                case StateScreaming: {
                    foreach (Player player in api.Players) {
                        player.AddExternalForce(-1.51f, 0f);
                    }
                    break;
                }
            }

            stateTime -= Time.TimeMult;
        }

        public override void OnHandleCollision(ActorBase other)
        {
            base.OnHandleCollision(other);

            if (state != StateDead) {
                // It can only die by collision with spring in the air
                Spring spring = other as Spring;
                if (spring != null) {
                    // Collide only with hitbox
                    if (AABB.TestOverlap(ref spring.AABBInner, ref AABBInner)) {
                        Vector2 force = spring.Activate();
                        int sign = ((force.X + force.Y) > float.Epsilon ? 1 : -1);
                        if (Math.Abs(force.X) > float.Epsilon) {
                            speedX = (4 + Math.Abs(force.X)) * sign;
                            externalForceX = force.X;
                        } else if (Math.Abs(force.Y) > float.Epsilon) {
                            speedY = (4 + Math.Abs(force.Y)) * sign;
                            externalForceY = -force.Y;
                        } else {
                            return;
                        }
                        canJump = false;

                        SetAnimation(AnimState.Fall);
                        PlaySound("Spring");

                        api.BroadcastLevelText(endText);

                        state = StateDead;
                        stateTime = 50f;
                    }
                }
            }
        }

        private void CheckDestructibleTiles()
        {
            TileMap tiles = api.TileMap;
            if (tiles == null) {
                return;
            }

            float timeMult = Time.TimeMult;
            AABB aabb = AABBInner + new Vector2((speedX + externalForceX) * 2f * timeMult, (speedY - externalForceY) * 2f * timeMult);

            if (tiles.CheckWeaponDestructible(ref aabb, WeaponType.Blaster, int.MaxValue) > 0) {
                api.ShakeCameraView(20f);
            }
        }

        private class Brick : EnemyBase
        {
            private float time = 50f;

            public override void OnActivated(ActorActivationDetails details)
            {
                base.OnActivated(details);

                base.isInvulnerable = true;
                base.collisionFlags = CollisionFlags.CollideWithOtherActors | CollisionFlags.ApplyGravitation | CollisionFlags.SkipPerPixelCollisions;

                health = 1;

                RequestMetadata("Boss/Queen");
                SetAnimation((AnimState)1073741829);

                PlaySound("BrickFalling", 0.3f);
            }

            protected override void OnUpdate()
            {
                base.OnUpdate();

                if (time <= 0f) {
                    DecreaseHealth(int.MaxValue);
                } else {
                    time -= Time.TimeMult;
                }
            }

            protected override bool OnPerish(ActorBase collider)
            {
                if (collider != null) {
                    CreateDeathDebris(collider);
                }

                return base.OnPerish(collider);
            }
        }

        private class InvisibleBlock : ActorBase
        {
            public override void OnActivated(ActorActivationDetails details)
            {
                base.OnActivated(details);

                collisionFlags = CollisionFlags.CollideWithOtherActors | CollisionFlags.IsSolidObject | CollisionFlags.SkipPerPixelCollisions;

                health = int.MaxValue;

                RequestMetadata("Boss/Queen");
                SetAnimation(AnimState.Idle);

                renderer.Active = false;
            }

            protected override void OnUpdate()
            {
                // Nothing to do...
            }

            public void UpdateBlock(Vector3 pos)
            {
                Transform.Pos = new Vector3(pos.X, pos.Y - 32, pos.Z);

                OnUpdateHitbox();
            }
        }
    }
}