using System.Threading.Tasks;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Solid
{
    public class GemBarrel : GenericContainer
    {
        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            Movable = true;

            collisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            GenerateContents(EventType.Gem, details.Params[0], 0);
            GenerateContents(EventType.Gem, details.Params[1], 1);
            GenerateContents(EventType.Gem, details.Params[2], 2);
            GenerateContents(EventType.Gem, details.Params[3], 3);

            await RequestMetadataAsync("Object/BarrelContainer");
            SetAnimation(AnimState.Idle);
        }

        public override void OnHandleCollision(ActorBase other)
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

            base.OnHandleCollision(other);
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