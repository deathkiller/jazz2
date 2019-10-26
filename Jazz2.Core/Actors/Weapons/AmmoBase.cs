using System.Threading.Tasks;
using Duality;
using Jazz2.Actors.Enemies;
using Jazz2.Actors.Solid;
using Jazz2.Game.Collisions;
using Jazz2.Game.Events;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;

namespace Jazz2.Actors.Weapons
{
    public abstract class AmmoBase : ActorBase
    {
        protected Player owner;
        protected float timeLeft;

        protected bool firedUp;
        protected byte upgrades;
        protected int strength;

        protected ActorBase lastRicochet;
        private int lastRicochetFrame;

        public Player Owner => owner;

        public int Strength => strength;

        public virtual WeaponType WeaponType => WeaponType.Unknown;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            collisionFlags = CollisionFlags.CollideWithTileset | CollisionFlags.CollideWithOtherActors | CollisionFlags.ApplyGravitation;
            canBeFrozen = false;
        }

        public override void OnFixedUpdate(float timeMult)
        {
            timeLeft -= timeMult;
            if (timeLeft <= 0) {
                DecreaseHealth();
            }
        }

        protected void TryMovement(float timeMult)
        {
            speedX = MathF.Clamp(speedX, -16f, 16f);
            speedY = MathF.Clamp(speedY - (internalForceY + externalForceY) * timeMult, -16f, 16f);

            float effectiveSpeedX, effectiveSpeedY;
            effectiveSpeedX = speedX + externalForceX * timeMult;
            effectiveSpeedY = speedY;
            effectiveSpeedX *= timeMult;
            effectiveSpeedY *= timeMult;

            MoveInstantly(new Vector2(effectiveSpeedX, effectiveSpeedY), MoveType.Relative, true);
        }

        protected void CheckCollisions(float timeMult)
        {
            if (health <= 0) {
                return;
            }

            TileMap tiles = api.TileMap;
            if (tiles != null) {
                AABB adjustedAABB = AABBInner + new Vector2(speedX * timeMult, speedY * timeMult);
                if (tiles.CheckWeaponDestructible(ref adjustedAABB, WeaponType, strength) > 0) {
                    if (WeaponType != WeaponType.Freezer) {
                        if (owner != null) {
                            owner.AddScore(50);
                        }
                    }

                    DecreaseHealth(1);
                } else if (!tiles.IsTileEmpty(ref AABBInner, false)) {
                    EventMap events = api.EventMap;
                    bool handled = false;
                    if (events != null) {
                        Vector3 pos = Transform.Pos;
                        ushort[] eventParams = null;
                        switch (events.GetEventByPosition(pos.X + speedX * timeMult, pos.Y + speedY * timeMult, ref eventParams)) {
                            case EventType.ModifierRicochet:
                                if (lastRicochetFrame + 2 < Time.FrameCount) {
                                    lastRicochet = null;
                                    lastRicochetFrame = Time.FrameCount;
                                    OnRicochet();
                                    handled = true;
                                }
                                break;
                        }
                    }

                    if (!handled) {
                        OnHitWall();
                    }
                }
            }
        }

        protected virtual void OnRicochet()
        {
            MoveInstantly(new Vector2(-speedX, -speedY), MoveType.Relative, true);

            speedY = speedY * -0.9f + (MathF.Rnd.Next() % 100 - 50) * 0.1f;
            speedX = speedX * -0.9f + (MathF.Rnd.Next() % 100 - 50) * 0.1f;
        }

        public override void OnHandleCollision(ActorBase other)
        {
            if (other is TriggerCrate || other is BarrelContainer || other is PowerUpWeaponMonitor) {
                if (lastRicochet != other) {
                    lastRicochet = other;
                    lastRicochetFrame = Time.FrameCount;
                    OnRicochet();
                } else if (lastRicochetFrame + 2 >= Time.FrameCount) {
                    DecreaseHealth(int.MaxValue);
                }
            } else if (other is EnemyBase && ((EnemyBase)other).CanCollideWithAmmo) {
                DecreaseHealth(int.MaxValue);
            } else if (other is SolidObjectBase) {
                DecreaseHealth(int.MaxValue);
            }
        }
    }
}