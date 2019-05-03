using Duality;
using Jazz2.Actors.Solid;

namespace Jazz2.Actors
{
    public class FrozenBlock : SolidObjectBase
    {
        private float timeLeft = 250f;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            collisionFlags = CollisionFlags.CollideWithOtherActors;
            canBeFrozen = false;

            RequestMetadata("Object/FrozenBlock");
            SetAnimation("FrozenBlock");
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            timeLeft -= Time.TimeMult;
            if (timeLeft <= 0) {
                DecreaseHealth(int.MaxValue);
            }
        }
    }
}