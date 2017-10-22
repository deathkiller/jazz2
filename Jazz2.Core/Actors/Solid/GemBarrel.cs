using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Solid
{
    public class GemBarrel : GenericContainer
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            Movable = true;

            collisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            GenerateContents(EventType.Gem, details.Params[0], 0);
            GenerateContents(EventType.Gem, details.Params[1], 1);
            GenerateContents(EventType.Gem, details.Params[2], 2);
            GenerateContents(EventType.Gem, details.Params[3], 3);

            RequestMetadata("Object/BarrelContainer");
            SetAnimation(AnimState.Idle);
        }

        public override void HandleCollision(ActorBase other)
        {
            switch (other) {
                case AmmoBase collision: {
                    if ((collision.WeaponType == WeaponType.RF ||
                         collision.WeaponType == WeaponType.Seeker ||
                         collision.WeaponType == WeaponType.Pepper ||
                         collision.WeaponType == WeaponType.Electro)) {

                        DecreaseHealth(collision.Strength, collision);
                        collision.DecreaseHealth(int.MaxValue);
                    }
                    break;
                }

                case AmmoTNT collision: {
                    DecreaseHealth(int.MaxValue, collision);
                    break;
                }

                case Player collision: {
                    if (collision.CanBreakSolidObjects) {
                        DecreaseHealth(int.MaxValue, collision);
                    }
                    break;
                }
            }

            base.HandleCollision(other);
        }

        protected override bool OnPerish(ActorBase collider)
        {
            PlaySound("Break");

            CreateParticleDebris();

            CreateSpriteDebris("BarrelShrapnel1", 3);
            CreateSpriteDebris("BarrelShrapnel2", 3);
            CreateSpriteDebris("BarrelShrapnel3", 2);
            CreateSpriteDebris("BarrelShrapnel4", 1);

            return base.OnPerish(collider);
        }
    }
}