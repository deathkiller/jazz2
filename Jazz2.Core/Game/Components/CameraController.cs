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
            // Z is not used
            //float z = focusPos.Z - camera.FocusDist; 
            //lastPos = new Vector3(x, y, z);
            lastPos = new Vector2(x, y);

            Vector2 halfView = LevelRenderSetup.TargetSize * 0.5f;

            Vector3 speed = targetObj.Speed;
            distanceFactor.X = MathF.Lerp(distanceFactor.X, speed.X * 8f, 0.2f * timeMult);
            distanceFactor.Y = MathF.Lerp(distanceFactor.Y, speed.Y * 5f, 0.04f * timeMult);

            // Clamp camera position to level bounds
            transform.Pos = new Vector3(
                MathF.Round(MathF.Clamp(lastPos.X + distanceFactor.X, viewRect.X + halfView.X, viewRect.RightX - halfView.X)),
                MathF.Round(MathF.Clamp(lastPos.Y + distanceFactor.Y, viewRect.Y + halfView.Y, viewRect.BottomY - halfView.Y)),
                0
            );
        }
    }
}