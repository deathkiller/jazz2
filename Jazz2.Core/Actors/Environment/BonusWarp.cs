using Duality;
using Jazz2.Game.Events;

namespace Jazz2.Actors.Environment
{
    public class BonusWarp : ActorBase
    {
        private ushort warpTarget, cost;

        public ushort Cost => cost;

        public Vector2 WarpTarget
        {
            get
            {
                EventMap events = api.EventMap;
                if (events == null) {
                    return new Vector2(-1, -1);
                } else {
                    return api.EventMap.GetWarpTarget(warpTarget);
                }
            }
        }

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            warpTarget = details.Params[0];
            cost = details.Params[3];

            canBeFrozen = false;

            RequestMetadata("Object/BonusWarp");

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
                    // ToDo: Ping-pong animation not supported for actors
                    SetAnimation("BonusGeneric");
                    break;
            }
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(28, 28);
        }
    }
}