using System.Threading.Tasks;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Solid
{
    public class PowerUpWeaponMonitor : SolidObjectBase
    {
        private WeaponType weaponType;

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Object/PowerUpMonitor");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new PowerUpWeaponMonitor();
            actor.OnActivated(details);
            return actor;
        }

        private PowerUpWeaponMonitor()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            weaponType = (WeaponType)details.Params[0];

            Movable = true;

            CollisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            await RequestMetadataAsync("Object/PowerUpMonitor");

            switch (weaponType) {
                case WeaponType.Blaster:
                    PlayerType player = (levelHandler.Players.Count == 0 ? PlayerType.Jazz : levelHandler.Players[0].PlayerType);
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

        public override void OnHandleCollision(ActorBase other)
        {
            if (health == 0) {
                return;
            }

            switch (other) {
                case AmmoBase collision: {
                    if ((collision.WeaponType == WeaponType.Blaster || 
                         collision.WeaponType == WeaponType.RF ||
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

            base.OnHandleCollision(other);
        }

        public void DestroyAndApplyToPlayer(Player player)
        {
            player.AddWeaponUpgrade(weaponType, 0x1);
            player.AddAmmo(weaponType, 5);

            DecreaseHealth(int.MaxValue, player);
            PlaySound(Transform.Pos, "Break");
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateParticleDebris();

            return base.OnPerish(collider);
        }
    }
}