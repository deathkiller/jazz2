using Duality;
using Duality.Components;
using Jazz2.Actors;

namespace Jazz2.Game
{
    public class CameraController : Component, ICmpFixedUpdatable
    {
        private ActorBase targetObj;
        private Rect viewBounds;
        private Rect viewBoundsTarget;

        private Vector2 distanceFactor;
        private Vector2 lastPos;

        private float shakeDuration;
        private Vector2 shakeOffset;

        public ActorBase TargetObject
        {
            get
            {
                return targetObj;
            }
            set
            {
                targetObj = value;
                if (targetObj != null && targetObj.Transform != null) {
                    lastPos = targetObj.Transform.Pos.Xy;

                    gameobj.Transform.Pos = new Vector3(lastPos, 0);
                }
            }
        }

        public Rect ViewBounds
        {
            get
            {
                return viewBoundsTarget;
            }
            set
            {
                viewBounds = value;
                viewBoundsTarget = value;
            }
        }

        void ICmpFixedUpdatable.OnFixedUpdate(float timeMult)
        {
            if (targetObj == null) {
                return;
            }

            // View Bounds Animation
            if (viewBounds != viewBoundsTarget) {
                if (MathF.Abs(viewBounds.X - viewBoundsTarget.X) < 2f) {
                    viewBounds = viewBoundsTarget;
                } else {
                    const float transitionSpeed = 0.02f;
                    float dx = (viewBoundsTarget.X - viewBounds.X) * transitionSpeed * timeMult;
                    viewBounds.X += dx;
                    viewBounds.W -= dx;
                }
            }

            Transform transform = GameObj.Transform;

            // The position to focus on
            Vector3 focusPos = targetObj.Transform.Pos;

            float x = MathF.Lerp(lastPos.X, focusPos.X, 0.5f * timeMult);
            float y = MathF.Lerp(lastPos.Y, focusPos.Y, 0.5f * timeMult);
            lastPos = new Vector2(x, y);

            Vector2 halfView = LevelRenderSetup.TargetSize * 0.5f;

            Vector3 speed = targetObj.Speed;
            distanceFactor.X = MathF.Lerp(distanceFactor.X, speed.X * 8f, 0.2f * timeMult);
            distanceFactor.Y = MathF.Lerp(distanceFactor.Y, speed.Y * 5f, 0.04f * timeMult);

            if (shakeDuration > 0f) {
                shakeDuration -= timeMult;

                if (shakeDuration <= 0f) {
                    shakeOffset = Vector2.Zero;
                } else {
                    float shakeFactor = 0.1f * timeMult;
                    shakeOffset.X = MathF.Lerp(shakeOffset.X, MathF.Rnd.NextFloat(-0.2f, 0.2f) * halfView.X, shakeFactor) * MathF.Min(shakeDuration * 0.1f, 1f);
                    shakeOffset.Y = MathF.Lerp(shakeOffset.Y, MathF.Rnd.NextFloat(-0.2f, 0.2f) * halfView.Y, shakeFactor) * MathF.Min(shakeDuration * 0.1f, 1f);
                }
            }

            // Clamp camera position to level bounds
            transform.Pos = new Vector3(
                MathF.Round(MathF.Clamp(lastPos.X + distanceFactor.X, viewBounds.X + halfView.X, viewBounds.RightX - halfView.X) + shakeOffset.X),
                MathF.Round(MathF.Clamp(lastPos.Y + distanceFactor.Y, viewBounds.Y + halfView.Y, viewBounds.BottomY - halfView.Y) + shakeOffset.Y),
                0
            );
        }

        public void Shake(float duration)
        {
            if (shakeDuration < duration) {
                shakeDuration = duration;
            }
        }

        public void AnimateToBounds(Rect bounds)
        {
            float viewWidth = LevelRenderSetup.TargetSize.X;

            if (bounds.W < viewWidth) {
                bounds.X -= (viewWidth - bounds.W);
                bounds.W = viewWidth;
            }

            viewBoundsTarget = bounds;

            float limit = GameObj.Transform.Pos.X - viewWidth * 0.6f;
            if (viewBounds.X < limit) {
                viewBounds.W += (viewBounds.X - limit);
                viewBounds.X = limit;
            }
        }
    }
}