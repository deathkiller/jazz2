using Duality;
using Jazz2.Actors.Solid;
using Jazz2.Game.Structs;

namespace Jazz2.Actors
{
    public class FrozenBlock : SolidObjectBase
    {
        private float ttl = 250;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            collisionFlags = CollisionFlags.CollideWithOtherActors;
            canBeFrozen = false;

            RequestMetadata("Object/FrozenBlock");
            SetAnimation(AnimState.Idle);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            ttl -= Time.TimeMult;
            if (ttl <= 0) {
                DecreaseHealth(int.MaxValue);
            }
        }
    }
}