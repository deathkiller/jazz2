using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors
{
    public class Eva : ActorBase
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            RequestMetadata("Object/Eva");
            SetAnimation(AnimState.Idle);

            collisionFlags &= ~CollisionFlags.ApplyGravitation;
        }

        // ToDo: Implement this
    }
}