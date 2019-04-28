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

        public override void OnHandleCollision(ActorBase other)
        {
            switch (other) {
                case Player player: {
                    if (player.PlayerType == PlayerType.Frog) {
                        player.MorphToOriginal();
                    }
                    break;
                }
            }
        }
    }
}