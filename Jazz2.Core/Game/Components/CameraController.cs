using Duality;
using Duality.Components;
using Jazz2.Actors;

namespace Jazz2.Game
{
    public class CameraController : Component, ICmpUpdatable
    {
        private ActorBase targetObj;
        private Rect viewRect;

        private Vector2 distanceFactor;
        private Vector2 lastPos;

        private float shakeDuration;
        private Vector3 shakeOffset;

        public ActorBase TargetObject
        {
            get { return targetObj; }
            set {
                targetObj = value;
                if (targetObj != null && targetObj.Transform != null) {
                    lastPos = targetObj.Transform.Pos.Xy;

                    gameobj.Transform.Pos = new Vector3(lastPos, 0);
                }
            }
        }

        public Rect ViewRect
        {
            get { return viewRect; }
            set { viewRect = value; }
        }

        void ICmpUpdatable.OnUpdate()
        {
            if (targetObj == null) {
                return;
            }

            Transform transform = GameObj.Transform;
            Camera camera = GameObj.GetComponent<Camera>();

            // The position to focus on
            Vector3 focusPos = targetObj.Transform.Pos;

            // Max. time multplier is set to 2 (~30 fps)
            float timeMult = MathF.Min(Time.TimeMult, 2f);

            float x = MathF.Lerp(lastPos.X, focusPos.X, 0.5f * timeMult);
            float y = MathF.Lerp(lastPos.Y, focusPos.Y, 0.5f * timeMult);
            lastPos = new Vector2(x, y);

            Vector2 halfView = LevelRenderSetup.TargetSize * 0.5f;

            Vector3 speed = targetObj.Speed;
            distanceFactor.X = MathF.Lerp(distanceFactor.X, speed.X * 8f, 0.2f * timeMult);
            distanceFactor.Y = MathF.Lerp(distanceFactor.Y, speed.Y * 5f, 0.04f * timeMult);

            if (shakeDuration > 0f) {
                shakeDuration -= Time.TimeMult;

                if (shakeDuration <= 0f) {
                    shakeOffset = Vector3.Zero;

                    transform.Angle = 0f;
                } else {
                    shakeOffset.X = MathF.Lerp(shakeOffset.X, MathF.Rnd.NextFloat(-0.06f, 0.06f) * halfView.X, 0.1f * timeMult);
                    shakeOffset.Y = MathF.Lerp(shakeOffset.Y, MathF.Rnd.NextFloat(-0.06f, 0.06f) * halfView.Y, 0.1f * timeMult);
                    shakeOffset.Z = MathF.Lerp(shakeOffset.Z, MathF.Rnd.NextFloat(-0.04f, 0.04f), 0.2f * timeMult);

                    transform.Angle = shakeOffset.Z;
                }
            }

            // Clamp camera position to level bounds
            transform.Pos = new Vector3(
                MathF.Round(MathF.Clamp(lastPos.X + distanceFactor.X + shakeOffset.X, viewRect.X + halfView.X, viewRect.RightX - halfView.X)),
                MathF.Round(MathF.Clamp(lastPos.Y + distanceFactor.Y + shakeOffset.Y, viewRect.Y + halfView.Y, viewRect.BottomY - halfView.Y)),
                0
            );
        }

        public void Shake(float duration)
        {
            shakeDuration = MathF.Max(shakeDuration, duration);
        }
    }
}