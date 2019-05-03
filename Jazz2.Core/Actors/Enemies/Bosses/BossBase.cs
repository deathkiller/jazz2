using Jazz2.Actors.Enemies;
using Jazz2.Game.Events;

namespace Jazz2.Actors.Bosses
{
    public abstract class BossBase : EnemyBase
    {
        public void DeactivateBoss()
        {
            if ((flags & (ActorInstantiationFlags.IsCreatedFromEventMap | ActorInstantiationFlags.IsFromGenerator)) != 0) {
                EventMap events = api.EventMap;
                if (events != null) {
                    if ((flags & ActorInstantiationFlags.IsFromGenerator) != 0) {
                        events.ResetGenerator(originTile.X, originTile.Y);
                    }

                    events.Deactivate(originTile.X, originTile.Y);
                }

                api.RemoveActor(this);
            }
        }

        public virtual void OnBossActivated()
        {
        }

        public override bool OnTileDeactivate(int tx1, int ty1, int tx2, int ty2)
        {
            return false;
        }
    }
}