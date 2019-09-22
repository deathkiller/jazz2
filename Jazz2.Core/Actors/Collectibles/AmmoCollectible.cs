using System.Threading.Tasks;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Collectibles
{
    public class AmmoCollectible : Collectible
    {
        private WeaponType weaponType;

        public override EventType EventType => EventType.Ammo;

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