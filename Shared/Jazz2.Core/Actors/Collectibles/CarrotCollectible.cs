﻿using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Collectibles
{
    public class CarrotCollectible : Collectible
    {
        private bool maxCarrot;

        public static void Preload(ActorActivationDetails details)
        {
            bool maxCarrot = (details.Params[0] != 0);

            if (maxCarrot) {
                PreloadMetadata("Collectible/CarrotFull");
            } else {
                PreloadMetadata("Collectible/Carrot");
            }
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new CarrotCollectible();
            actor.OnActivated(details);
            return actor;
        }

        private CarrotCollectible()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await base.OnActivatedAsync(details);

            maxCarrot = (details.Params[0] != 0);

            if (maxCarrot) {
                scoreValue = 500;
                await RequestMetadataAsync("Collectible/CarrotFull");
            } else {
                scoreValue = 200;
                await RequestMetadataAsync("Collectible/Carrot");
            }

            SetAnimation("Carrot");

            SetFacingDirection();
        }

        protected override void Collect(Player player)
        {
            if (maxCarrot) {
                player.AddHealth(-1);
                player.SetInvulnerability(5 * Time.FramesPerSecond, true);
                base.Collect(player);
            } else {
                if (player.AddHealth(1)) {
                    base.Collect(player);
                }
            }
        }
    }
}