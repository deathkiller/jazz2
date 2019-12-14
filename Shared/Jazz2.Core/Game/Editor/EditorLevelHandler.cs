#if ENABLE_EDITOR

using Duality;
using Duality.Components;
using Jazz2.Actors;
using Jazz2.Game.Structs;

namespace Jazz2.Game.Editor
{
    public class EditorLevelHandler : LevelHandler
    {
        public enum CameraAction
        {
            None,
            Move,
            DragScene,
        }

        private bool camTransformChanged;
        private Vector3 camVel;
        private Point2 camActionBeginLoc;
        private CameraAction camAction;

        public EditorLevelHandler(App root, LevelInitialization data) : base(root, data)
        {

        }

        protected override void OnDisposing(bool manually)
        {

            base.OnDisposing(manually);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

        }

        public override bool HandlePlayerDied(Player player)
        {

            return false;
        }

        public override void BroadcastTriggeredEvent(EventType eventType, ushort[] eventParams)
        {

            base.BroadcastTriggeredEvent(eventType, eventParams);
        }

        public override void AddActor(ActorBase actor)
        {
            base.AddActor(actor);

        }

        public override void RemoveActor(ActorBase actor)
        {
            base.RemoveActor(actor);

        }

        private void HandlerCameraMovement()
        {
            if (cameras.Count < 1) {
                return;
            }


            GameObject camObj = cameras[0];
            Camera cam = camObj.GetComponent<Camera>();

            Point2 cursorPos = DualityApp.Mouse.WindowPos;

            float unscaledTimeMult = Time.TimeMult / Time.TimeScale;

            this.camTransformChanged = false;

            if (this.camAction == CameraAction.DragScene) {
                Vector2 curPos = new Vector2(cursorPos.X, cursorPos.Y);
                Vector2 lastPos = new Vector2(this.camActionBeginLoc.X, this.camActionBeginLoc.Y);
                this.camActionBeginLoc = new Point2((int)curPos.X, (int)curPos.Y);

                float refZ = 0.0f;
                if (camObj.Transform.Pos.Z >= refZ - cam.NearZ)
                    refZ = camObj.Transform.Pos.Z + MathF.Abs(cam.FocusDist);

                Vector2 targetOff = (-(curPos - lastPos) / cam.GetScaleAtZ(refZ));
                Vector2 targetVel = targetOff / unscaledTimeMult;
                MathF.TransformCoord(ref targetVel.X, ref targetVel.Y, camObj.Transform.Angle);
                this.camVel.Z *= MathF.Pow(0.9f, unscaledTimeMult);
                this.camVel += (new Vector3(targetVel, this.camVel.Z) - this.camVel) * unscaledTimeMult;
                this.camTransformChanged = true;
            } else if (this.camAction == CameraAction.Move) {
                Vector3 moveVec = new Vector3(
                    cursorPos.X - this.camActionBeginLoc.X,
                    cursorPos.Y - this.camActionBeginLoc.Y,
                    this.camVel.Z);

                const float BaseSpeedCursorLen = 25.0f;
                const float BaseSpeed = 3.0f;
                moveVec.X = BaseSpeed * MathF.Sign(moveVec.X) * MathF.Pow(MathF.Abs(moveVec.X) / BaseSpeedCursorLen, 1.5f);
                moveVec.Y = BaseSpeed * MathF.Sign(moveVec.Y) * MathF.Pow(MathF.Abs(moveVec.Y) / BaseSpeedCursorLen, 1.5f);

                MathF.TransformCoord(ref moveVec.X, ref moveVec.Y, camObj.Transform.Angle);

                /*if (this.camBeginDragScene) {
                    float refZ = 0.0f;
                    if (camObj.Transform.Pos.Z >= refZ - cam.NearZ)
                        refZ = camObj.Transform.Pos.Z + MathF.Abs(cam.FocusDist);
                    moveVec = new Vector3(moveVec.Xy * 0.5f / cam.GetScaleAtZ(refZ), moveVec.Z);
                }*/

                this.camVel = moveVec;
                this.camTransformChanged = true;
            } else if (this.camVel.Length > 0.01f) {
                this.camVel *= MathF.Pow(0.9f, unscaledTimeMult);
                this.camTransformChanged = true;
            } else {
                this.camTransformChanged = this.camTransformChanged || (this.camVel != Vector3.Zero);
                this.camVel = Vector3.Zero;
            }

            if (this.camTransformChanged) {
                camObj.Transform.MoveBy(this.camVel * unscaledTimeMult);
            }
        }
    }
}

#endif