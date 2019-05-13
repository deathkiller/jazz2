using Jazz2.Game.Structs;

namespace Jazz2.Actors.Collectibles
{
    public class AmmoCollectible : Collectible
    {
        private WeaponType weaponType;

        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

            weaponType = (WeaponType)details.Params[0];

            scoreValue = 100;

            RequestMetadata("Collectible/Ammo" + weaponType.ToString("G"));
            SetAnimation("Ammo");

            SetFacingDirection();
        }

        protected override void Collect(Player player)
        {
            if (player.AddAmmo(weaponType, 3)) {
                base.Collect(player);
            }
        }
    }
}