using System.Threading.Tasks;
using Duality;
using Jazz2.Actors.Solid;
using Jazz2.Game.Collisions;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Environment
{
    public class PinballPaddle : SolidObjectBase
    {
        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Object/PinballPaddle");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new PinballPaddle();
            actor.OnActivated(details);
            return actor;
        }

        private PinballPaddle()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            IsFacingLeft = (details.Params[0] != 0);

            CollisionFlags = CollisionFlags.CollideWithOtherActors;

            await RequestMetadataAsync("Object/PinballPaddle");

            SetAnimation(AnimState.Idle);
        }

        protected override void OnUpdateHitbox()
        {
            if (currentAnimation != null) {
                Vector3 pos = Transform.Pos;
                AABBInner = new AABB(
                    pos.X - currentAnimation.Base.FrameDimensions.X * (IsFacingLeft ? 0.7f : 0.3f),
                    pos.Y - currentAnimation.Base.FrameDimensions.Y * 0.1f,
                    pos.X + currentAnimation.Base.FrameDimensions.X * (IsFacingLeft ? 0.3f : 0.7f),
                    pos.Y + currentAnimation.Base.FrameDimensions.Y * 0.3f
                );
            }
        }

        public Vector2 Activate(ActorBase other)
        {
            Player collider = other as Player;
            if (collider != null && Transform.Pos.Y > collider.Transform.Pos.Y) {
                if (currentTransitionState == AnimState.Idle) {
                    float selfX = Transform.Pos.X;
                    float colliderX = collider.Transform.Pos.X;

                    float mult = (colliderX - selfX) / currentAnimation.Base.FrameDimensions.X;
                    if (IsFacingLeft) {
                        mult = 1 - mult;
                    }
                    mult = MathF.Clamp(mult * 1.6f, 0.4f, 1f);

                    float force = 1.9f * mult;
                    collider.AddExternalForce(0f, force);

                    SetTransition(AnimState.TransitionActivate, false);
                    return new Vector2(0f, -1f);
                }
                
            }

            return Vector2.Zero;
        }
    }
}