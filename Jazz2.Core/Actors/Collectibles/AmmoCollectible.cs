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

            SetAnimation("Ammo" + weaponType.ToString("G"));
        }

        protected override void Collect(Player player)
        {
            if (player.AddAmmo(weaponType, 3)) {
                base.Collect(player);
            }
        }
    }
}