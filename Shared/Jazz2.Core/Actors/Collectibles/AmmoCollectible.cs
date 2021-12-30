using System.Threading.Tasks;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Collectibles
{
    public class AmmoCollectible : Collectible
    {
        private WeaponType weaponType;

        public static void Preload(ActorActivationDetails details)
        {
            WeaponType weaponType = (WeaponType)details.Params[0];

            PreloadMetadata("Collectible/Ammo" + weaponType.ToString("G"));
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new AmmoCollectible();
            actor.OnActivated(details);
            return actor;
        }

        private AmmoCollectible()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await base.OnActivatedAsync(details);

            weaponType = (WeaponType)details.Params[0];

            scoreValue = 100;

            await RequestMetadataAsync("Collectible/Ammo" + weaponType.ToString("G"));
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