using System.Collections.Generic;
using Duality;
using Jazz2.Actors.Enemies;
using Jazz2.Actors.Solid;
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

        public Player Owner => owner;

        public int Strength => strength;

        public virtual WeaponType WeaponType => WeaponType.Unknown;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            collisionFlags = CollisionFlags.CollideWithTileset | CollisionFlags.CollideWithOtherActors | CollisionFlags.ApplyGravitation;
            canBeFrozen = false;
        }

        protected override void OnUpdate()
        {
            timeLeft -= Time.TimeMult;
            if (timeLeft <= 0) {
                DecreaseHealth();
            }
        }

        protected void CheckCollisions()
        {
            TileMap tiles = api.TileMap;
            if (tiles != null) {
                float timeMult = Time.TimeMult;

                Hitbox hitbox = currentHitbox + new Vector2(speedX * timeMult, speedY * timeMult);

                if (tiles.CheckWeaponDestructible(ref hitbox, WeaponType, strength) > 0) {
                    if (WeaponType != WeaponType.Freezer) {
                        if (owner != null) {
                            owner.AddScore(50);
                        }
                    }

                    DecreaseHealth(1);
                } else if (!tiles.IsTileEmpty(ref hitbox, false)) {
                    EventMap events = api.EventMap;
                    if (events != null) {
                        Vector3 pos = Transform.Pos;
                        ushort[] eventParams = null;
                        switch (events.GetEventByPosition(pos.X + speedX * timeMult, pos.Y + speedY * timeMult, ref eventParams)) {
                            case EventType.ModifierRicochet:
                                lastRicochet = null;
                                OnRicochet();
                                break;
                        }
                    }
                }
            }
        }

        protected virtual void OnRicochet()
        {
            speedY = speedY * -0.9f + (MathF.Rnd.Next() % 100 - 50) * 0.1f;
            speedX = speedX * -0.9f + (MathF.Rnd.Next() % 100 - 50) * 0.1f;
        }

        public override void OnHandleCollision(ActorBase other)
        {
            if (other is TriggerCrate || other is BarrelContainer || other is PowerUpWeaponMonitor) {
                if (lastRicochet != other) {
                    lastRicochet = other;
                    OnRicochet();
                }
            } else if (other is EnemyBase || other is SolidObjectBase) {
                DecreaseHealth(int.MaxValue);
            }
        }
    }
}