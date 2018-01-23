using Duality;
using Jazz2.Actors.Solid;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Environment
{
    public class PinballPaddle : SolidObjectBase
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            IsFacingLeft = (details.Params[0] != 0);

            collisionFlags = CollisionFlags.CollideWithOtherActors;

            RequestMetadata("Object/PinballPaddle");

            SetAnimation(AnimState.Idle);
        }

        protected override void OnUpdateHitbox()
        {
            if (currentAnimation != null) {
                Vector3 pos = Transform.Pos;
                currentHitbox = new Hitbox(
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