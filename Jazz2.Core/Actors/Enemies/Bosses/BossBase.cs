using Jazz2.Actors.Enemies;

namespace Jazz2.Actors.Bosses
{
    public abstract class BossBase : EnemyBase
    {
        public virtual void OnBossActivated()
        {
        }

        public override bool OnTileDeactivate(int tx, int ty, int tileDistance)
        {
            return false;
        }
    }
}