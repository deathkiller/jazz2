using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Solid
{
    public class PowerUpShieldMonitor : SolidObjectBase
    {
        private ushort shieldType;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            shieldType = details.Params[0];

            Movable = true;

            collisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            RequestMetadata("Object/PowerUpMonitor");

            switch (shieldType) {
                case 0: SetAnimation("ShieldFire"); break;
                case 1: SetAnimation("ShieldWater"); break;
                case 2: SetAnimation("ShieldLaser"); break;
                case 3: SetAnimation("ShieldLightning"); break;

                default: SetAnimation("Empty"); break;
            }
        }

        public override void HandleCollision(ActorBase other)
        {
            if (health == 0) {
                return;
            }

            switch (other) {
                case AmmoBase collision: {
                    if ((collision.WeaponType == WeaponType.RF ||
                            collision.WeaponType == WeaponType.Seeker ||
                            collision.WeaponType == WeaponType.Pepper ||
                            collision.WeaponType == WeaponType.Electro) &&
                        collision.Owner != null) {

                        DestroyAndApplyToPlayer(collision.Owner);
                        collision.DecreaseHealth(int.MaxValue);
                    }
                    break;
                }

                case AmmoTNT collision: {
                    if (collision.Owner != null) {
                        DestroyAndApplyToPlayer(collision.Owner);
                    }
                    break;
                }

                case Player collision: {
                    if (collision.CanBreakSolidObjects) {
                        DestroyAndApplyToPlayer(collision);
                    }
                    break;
                }
            }

            base.HandleCollision(other);
        }

        public void DestroyAndApplyToPlayer(Player player)
        {
            // ToDo

            DecreaseHealth(int.MaxValue, player);
            PlaySound("Break");
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateParticleDebris();

            return base.OnPerish(collider);
        }
    }
}