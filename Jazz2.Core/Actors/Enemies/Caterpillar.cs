using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Caterpillar : EnemyBase
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            canBeFrozen = false;
            isFacingLeft = true;
            collisionFlags = CollisionFlags.CollideWithTileset | CollisionFlags.ApplyGravitation;
            canHurtPlayer = false;

            RequestMetadata("Enemy/Caterpillar");
            SetAnimation(AnimState.Idle);
        }

        // TODO: Implement this
    }
}