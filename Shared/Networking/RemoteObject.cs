using Duality;
using Jazz2.Actors;
using Jazz2.Game.Structs;

namespace Jazz2.Game.Multiplayer
{
    public class RemoteObject : ActorBase
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            // ToDo: Load metadata
            RequestMetadata("Weapon/Blaster");

            SetAnimation(AnimState.Fall);

            collisionFlags = CollisionFlags.CollideWithOtherActors;
        }

        public void UpdateFromServer(Vector3 pos, Vector2 speed, AnimState animState, float animTime, bool isFacingLeft)
        {
            Transform.Pos = pos;

            speedX = speed.X;
            speedY = speed.Y;

            if (availableAnimations != null) {
                if (currentAnimationState != animState) {
                    SetAnimation(animState);
                }

                renderer.AnimTime = animTime;
                IsFacingLeft = isFacingLeft;
            }
        }
    }
}