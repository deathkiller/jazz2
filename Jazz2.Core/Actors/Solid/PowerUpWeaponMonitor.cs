using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Solid
{
    public class PowerUpWeaponMonitor : SolidObjectBase
    {
        private WeaponType weaponType;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            weaponType = (WeaponType)details.Params[0];

            Movable = true;

            collisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            RequestMetadata("Object/PowerUpMonitor");

            switch (weaponType) {
                case WeaponType.Blaster:
                    PlayerType player = (api.Players.Count == 0 ? PlayerType.Jazz : api.Players[0].PlayerType);
                    if (player == PlayerType.Spaz) {
                        SetAnimation("BlasterSpaz");
                    } else if (player == PlayerType.Lori) {
                        SetAnimation("BlasterLori");
                    } else {
                        SetAnimation("BlasterJazz");
                    } 
                    break;

                default: SetAnimation(weaponType.ToString("G")); break;
            }
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
                     collider.WeaponType == WeaponType.Electro) &&
                    collider.Owner != null) {

                    DestroyAndApplyToPlayer(collider.Owner);
                    collider.DecreaseHealth(int.MaxValue);
                }
            }

            base.HandleCollision(other);
        }

        public void DestroyAndApplyToPlayer(Player player)
        {
            player.AddWeaponUpgrade(weaponType, 0x1);
            player.AddAmmo(weaponType, 5);

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