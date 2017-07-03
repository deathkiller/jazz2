using Jazz2.Game.Structs;

namespace Jazz2.Actors.Collectibles
{
    public class AmmoCollectible : Collectible
    {
        private WeaponType weaponType;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            weaponType = (WeaponType)details.Params[0];

            scoreValue = 100;

            string spriteName;
            switch (weaponType) {
                case WeaponType.Bouncer: spriteName = "BOUNCER"; break;
                case WeaponType.Freezer: spriteName = "FREEZER"; break;
                case WeaponType.Seeker: spriteName = "SEEKER"; break;
                case WeaponType.RF: spriteName = "RF"; break;
                case WeaponType.Toaster: spriteName = "TOASTER"; break;
                case WeaponType.TNT: spriteName = "TNT"; break;
                case WeaponType.Pepper: spriteName = "PEPPER"; break;
                case WeaponType.Electro: spriteName = "ELECTRO"; break;

                default: return;
            }

            SetAnimation("PICKUP_AMMO_" + spriteName);
        }

        public override void Collect(Player player) {
            if (player.AddAmmo(weaponType, 3)) {
                base.Collect(player);
            }
        }
    }
}