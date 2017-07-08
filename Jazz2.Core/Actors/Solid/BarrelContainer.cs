using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Solid
{
    public class BarrelContainer : GenericContainer
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            Movable = true;

            collisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            if (details.Params[0] != 0 && details.Params[1] != 0) {
                GenerateContents((EventType)details.Params[0], details.Params[1], details.Params[2], details.Params[3],
                    details.Params[4], details.Params[5], details.Params[6], details.Params[7]);
            }

            RequestMetadata("Object/BarrelContainer");
            SetAnimation(AnimState.Idle);
        }

        public override void HandleCollision(ActorBase other)
        {
            if (health == 0) {
                return;
            }

            AmmoBase collider = other as AmmoBase;
            if (collider != null) {
                if ((collider.WeaponType == WeaponType.RF ||
                     collider.WeaponType == WeaponType.Seeker ||
                     collider.WeaponType == WeaponType.Pepper ||
                     collider.WeaponType == WeaponType.Electro)) {

                    DecreaseHealth(int.MaxValue);
                    collider.DecreaseHealth(int.MaxValue);
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