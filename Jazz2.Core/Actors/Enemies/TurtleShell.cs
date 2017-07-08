using System.Collections.Generic;
using Duality;
using Jazz2.Actors.Collectibles;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;

namespace Jazz2.Actors.Enemies
{
    public class TurtleShell : EnemyBase
    {
        public TurtleShell()
        {
            // Empty constructor for spawning
        }

        public TurtleShell(float speedX, float speedY)
        {
            this.speedX = speedX;
            this.externalForceY = speedY;
        }

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            ushort theme = details.Params[0];

            switch (theme) {
                case 0:
                default:
                    RequestMetadata("Enemy/TurtleShell");
                    break;

                case 1: // Xmas
                    RequestMetadata("Enemy/TurtleShellXmas");
                    break;
            }

            SetAnimation(AnimState.Idle);

            canHurtPlayer = false;
            friction = api.Gravity / 100;
            elasticity = 0.5f;
            // ToDo: Test the actual number
            health = 8;

            //collisionFlags |= CollisionFlags.CollideWithSolidObjects;

            PlaySound("ENEMY_TURTLE_SHELL_FLY");
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(24, 16);
        }

        protected override void OnUpdate()
        {
            speedX = MathF.Max(MathF.Abs(speedX) - friction, 0f) * (speedX > float.Epsilon ? 1f : -1f);

            double posYBefore = Transform.Pos.Y;
            base.OnUpdate();

            Vector3 pos = Transform.Pos;
            if (posYBefore - pos.Y > 0.5 && MathF.Abs(speedY) < 1) {
                speedX = MathF.Max(MathF.Abs(speedX) - 10f * friction, 0f) * (speedX > float.Epsilon ? 1f : -1f);
            }

            List<ActorBase> collisions = api.FindCollisionActors(this);
            for (int i = 0; i < collisions.Count; i++) {
                {
                    TurtleShell collider = collisions[i] as TurtleShell;
                    if (collider != null) {
                        if (speedY - collider.speedY > 1f && speedY > 0f) {
                            collider.DecreaseHealth(10, this);
                            continue;
                        }

                        if (MathF.Abs(speedX) > MathF.Abs(collider.speedX)) {
                            // Handle this only in the faster of the two.
                            pos.X = collider.Transform.Pos.X + (speedX > 0 ? -1f : 1f) * (currentAnimation.FrameDimensions.X + 1);
                            float totalSpeed = MathF.Abs(speedX) + MathF.Abs(collider.speedX);

                            collider.speedX = totalSpeed / 2 * (speedX > 0f ? 1f : -1f);
                            speedX = totalSpeed / 2f * (speedX > 0f ? -1f : 1f);

                            collider.DecreaseHealth(1, this);
                            PlaySound("ENEMY_TURTLE_SHELL_IMPACT_SHELL", 0.8f);
                        }
                        continue;
                    }
                }

                {
                    AmmoBase collider = collisions[i] as AmmoBase;
                    if (collider != null) {
                        PlaySound("ENEMY_TURTLE_SHELL_FLY");
                        continue;
                    }
                }

                {
                    EnemyBase collider = collisions[i] as EnemyBase;
                    if (collider != null && MathF.Abs(speedX) > 3f) {
                        collider.DecreaseHealth(1, this);
                        speedX = MathF.Max(MathF.Abs(speedX), 2f) * (speedX > 0f ? -1f : 1f);
                        continue;
                    }
                }

                {
                    Collectible collider = collisions[i] as Collectible;
                    if (collider != null) {
                        collider.HandleCollision(this);
                        continue;
                    }
                }
            }

            TileMap tiles = api.TileMap;
            if (tiles != null) {
                tiles.CheckSpecialDestructible(ref currentHitbox);
                tiles.CheckCollapseDestructible(ref currentHitbox);
                tiles.CheckWeaponDestructible(ref currentHitbox, WeaponType.Blaster, 1);
            }
        }

        public override void HandleCollision(ActorBase other)
        {
            base.HandleCollision(other);

            if (other is AmmoBase && !(other is AmmoFreezer)) {
                if (other is AmmoToaster) {
                    DecreaseHealth(int.MaxValue, other);
                    return;
                }

                float otherSpeed = other.Speed.X;
                speedX = MathF.Max(4f, MathF.Abs(otherSpeed)) * (otherSpeed < 0f ? -0.5f : 0.5f);
            }
        }

        protected override void OnHitFloorHook()
        {
            if (MathF.Abs(speedY) > 1f) {
                PlaySound("ENEMY_TURTLE_SHELL_IMPACT_GROUND");
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