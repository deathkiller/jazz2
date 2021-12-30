﻿using System.Threading.Tasks;
using Duality;
using Jazz2.Game;
using Jazz2.Game.Events;

namespace Jazz2.Actors.Environment
{
    public class BonusWarp : ActorBase
    {
        private ushort warpTarget, cost;
        private bool setLaps;
        private bool fast;

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Object/BonusWarp");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new BonusWarp();
            actor.OnActivated(details);
            return actor;
        }

        private BonusWarp()
        {
        }

        public ushort Cost => cost;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            warpTarget = details.Params[0];
            fast = (details.Params[1] != 0);
            setLaps = details.Params[2] != 0;
            cost = details.Params[3];
            // ToDo: Show rabbit for non-listed number of coins (use JJ2+ anim set 8)
            //showAnim = details.Params[4] != 0;

            canBeFrozen = false;

            await RequestMetadataAsync("Object/BonusWarp");

            switch (cost) {
                case 10:
                    SetAnimation("Bonus10");
                    break;
                case 20:
                    SetAnimation("Bonus20");
                    break;
                case 50:
                    SetAnimation("Bonus50");
                    break;
                case 100:
                    SetAnimation("Bonus100");
                    break;
                default:
                    // ToDo: Show rabbit + coins needed, if (showAnim)
                    SetAnimation("BonusGeneric");
                    break;
            }
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(28, 28);
        }

        public void Activate(Player player)
        {
            EventMap events = levelHandler.EventMap;
            if (events == null) {
                return;
            }

            Vector2 targetPos = events.GetWarpTarget(warpTarget);
            if (targetPos.X < 0f || targetPos.Y < 0f) {
                // Warp target not found
                return;
            }

            player.WarpToPosition(targetPos, fast);

#if MULTIPLAYER && SERVER
            ((LevelHandler)levelHandler).OnPlayerIncrementLaps(player);
#endif
        }
    }
}