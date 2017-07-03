using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class BeeSwarm : EnemyBase
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            isGravityAffected = false;
            isCollidable = false;
            canHurtPlayer = false;

            RequestMetadata("Enemy/BeeSwarm");
            SetAnimation(AnimState.IDLE);
        }

        // TODO: What's this and where it's used?
    }
}