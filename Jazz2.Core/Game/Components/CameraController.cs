using Duality;
using Duality.Components;
using Jazz2.Actors;

namespace Jazz2.Game
{
    public class CameraController : Component, ICmpUpdatable
    {
        //private float smoothness = 1.0f;
        //private float thresholdDist = 6.0f;
        private ActorBase targetObj;
        private Rect viewRect;

        private Vector2 distanceFactor;
        private Vector3 lastPos;

        /// <summary>
        /// [GET / SET] How smooth the camera should follow its target.
        /// </summary>
        //public float Smoothness
        //{
        //    get { return smoothness; }
        //    set { smoothness = value; }
        //}
        /// <summary>
		/// [GET / SET] The distance threshold that needs to be exceeded before the camera starts to move.
		/// </summary>
		//public float ThresholdDist
        //{
        //    get { return thresholdDist; }
        //    set { thresholdDist = value; }
        //}
        public ActorBase TargetObject
        {
            get { return targetObj; }
            set {
                targetObj = value;
                if (targetObj != null && targetObj.Transform != null) {
                    lastPos = targetObj.Transform.Pos;
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

            // The position to focus on.
            Vector3 focusPos = targetObj.Transform.Pos;
            // The position where the camera itself should move
            Vector3 targetPos = (focusPos - new Vector3(0.0f, 0.0f, camera.FocusDist) + lastPos) * 0.5f;
            // A relative movement vector that would place the camera directly at its target position.
            //Vector3 posDiff = (targetPos - transform.Pos);
            // Add a threshold to the position difference, before it gets noticed by the camera
            //if (thresholdDist > 0) {
            //    float posDiffLength = posDiff.Length;
            //    Vector3 posDiffDir = posDiff / MathF.Max(posDiffLength, 0.01f);
            //    posDiffLength = MathF.Max(0.0f, posDiffLength - thresholdDist);
            //    posDiff = posDiffDir * posDiffLength;
            //}
            // A relative movement vector that doesn't go all the way, but just a bit towards its target.
            //Vector3 targetVelocity = posDiff * 0.1f * MathF.Pow(2.0f, -smoothness);

            //Vector3 targetVelocityAdjusted = targetVelocity * Time.TimeMult;

            lastPos = targetPos;

            Vector2 halfView = LevelRenderSetup.TargetSize * 0.5f;

            // Clamp camera position to level bounds
            //transform.Pos = new Vector3(
            //    MathF.Round(MathF.Clamp(transform.Pos.X + targetVelocityAdjusted.X, viewRect.X + halfView.X, viewRect.RightX - halfView.X)),
            //    MathF.Round(MathF.Clamp(transform.Pos.Y + targetVelocityAdjusted.Y, viewRect.Y + halfView.Y, viewRect.BottomY - halfView.Y)),
            //    0
            //);

            Vector3 speed = targetObj.Speed;
            distanceFactor.X = MathF.Lerp(distanceFactor.X, speed.X * /*6f*/8f, 0.2f);
            distanceFactor.Y = MathF.Lerp(distanceFactor.Y, speed.Y * 5f, 0.04f);

            transform.Pos = new Vector3(
                MathF.Round(MathF.Clamp(targetPos.X + distanceFactor.X, viewRect.X + halfView.X, viewRect.RightX - halfView.X)),
                MathF.Round(MathF.Clamp(targetPos.Y + distanceFactor.Y, viewRect.Y + halfView.Y, viewRect.BottomY - halfView.Y)),
                0
            );
        }
    }
}