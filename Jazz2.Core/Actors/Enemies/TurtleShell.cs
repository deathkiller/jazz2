using Duality;
using Jazz2.Actors.Collectibles;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;

namespace Jazz2.Actors.Enemies
{
    public class TurtleShell : EnemyBase
    {
        private float lastAngle;

        public TurtleShell()
        {
            // Empty constructor for spawning
        }

        public TurtleShell(float speedX, float speedY)
        {
            this.speedX = speedX;
            this.externalForceY = speedY;
        }

        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

            ushort theme = details.Params[0];

            switch (theme) {
                case 0:
                default: // Normal
                    RequestMetadata("Enemy/TurtleShell");
                    break;

                case 1: // Xmas
                    RequestMetadata("Enemy/TurtleShellXmas");
                    break;

                case 2: // Tough (Boss)
                    RequestMetadata("Boss/TurtleShellTough");
                    break;
            }

            SetAnimation(AnimState.Idle);

            canHurtPlayer = false;
            friction = api.Gravity * 0.05f;
            elasticity = 0.5f;
            health = 8;

            PlaySound("Fly");
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(24, 16);
        }

        protected override void OnUpdate()
        {
            speedX = MathF.Max(MathF.Abs(speedX) - friction, 0f) * (speedX < 0f ? -1f : 1f);

            double posYBefore = Transform.Pos.Y;
            base.OnUpdate();

            Vector3 pos = Transform.Pos;
            if (posYBefore - pos.Y > 0.5 && MathF.Abs(speedY) < 1) {
                speedX = MathF.Max(MathF.Abs(speedX) - 10f * friction, 0f) * (speedX < 0f ? -1f : 1f);
            }

            TileMap tiles = api.TileMap;
            if (tiles != null) {
                tiles.CheckSpecialDestructible(ref AABBInner);
                tiles.CheckCollapseDestructible(ref AABBInner);
                tiles.CheckWeaponDestructible(ref AABBInner, WeaponType.Blaster, 1);
            }

            lastAngle = MathF.Lerp(lastAngle, speedX * 0.06f, Time.TimeMult * 0.2f);
            if (MathF.Abs(Transform.Angle - MathF.NormalizeAngle(lastAngle)) > 0.01f) {
                Transform.Angle = lastAngle;
            }
        }

        public override void OnHandleCollision(ActorBase other)
        {
            base.OnHandleCollision(other);

            switch (other) {
                case AmmoBase ammo: {
                    if (ammo is AmmoFreezer) {
                        break;
                    }

                    if (other is AmmoToaster) {
                        DecreaseHealth(int.MaxValue, other);
                        return;
                    }

                    float otherSpeed = other.Speed.X;
                    speedX = MathF.Max(4f, MathF.Abs(otherSpeed)) * (otherSpeed < 0f ? -0.5f : 0.5f);

                    PlaySound("Fly");
                    break;
                }

                case TurtleShell shell: {
                    if (speedY - shell.speedY > 1f && speedY > 0f) {
                        shell.DecreaseHealth(10, this);
                    } else if (MathF.Abs(speedX) > MathF.Abs(shell.speedX)) {
                        // Handle this only in the faster of the two
                        //pos.X = collider.Transform.Pos.X + (speedX >= 0 ? -1f : 1f) * (currentAnimation.Base.FrameDimensions.X + 1);
                        float totalSpeed = MathF.Abs(speedX) + MathF.Abs(shell.speedX);

                        shell.speedX = totalSpeed / 2 * (speedX >= 0f ? 1f : -1f);
                        speedX = totalSpeed / 2f * (speedX >= 0f ? -1f : 1f);

                        shell.DecreaseHealth(1, this);
                        PlaySound("ImpactShell", 0.8f);
                    }
                    break;
                }

                case EnemyBase enemy: {
                    if (enemy.CanCollideWithAmmo) {
                        if (!enemy.IsInvulnerable) {
                            enemy.DecreaseHealth(1, this);
                        }
                        
                        speedX = MathF.Max(MathF.Abs(speedX), 2f) * (speedX >= 0f ? -1f : 1f);
                    }
                    break;
                }

                //case Collectible collectible: {
                //    collectible.OnHandleCollision(this);
                //    break;
                //}
            }
        }

        protected override void OnHitFloor()
        {
            if (MathF.Abs(speedY) > 1f) {
                PlaySound("ImpactGround");
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