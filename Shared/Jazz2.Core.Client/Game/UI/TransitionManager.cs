using Duality;
using Duality.Drawing;
using Duality.Resources;

namespace Jazz2.Game.UI
{
    public class TransitionManager
    {
        public enum Mode
        {
            None,
            FadeIn,
            FadeOut
        }

        private readonly Mode mode;
        private bool isCompleted;
        private float time;
        private float timeIncrement;
        private ContentRef<Material> material;

        public Mode CurrentMode => mode;
        public bool IsCompleted => isCompleted;

        public TransitionManager(Mode mode, bool smooth)
        {
            this.mode = mode;

            if (smooth) {
                material = new Material(ContentResolver.Current.RequestShader("TransitionSmooth"));
                timeIncrement = 1 / 30f;
            } else {
                material = new Material(ContentResolver.Current.RequestShader("Transition"));
                timeIncrement = 1 / 50f;
            }
        }

        public void Draw(IDrawDevice device)
        {
            if (mode == Mode.None) {
                return;
            }

            float progressTime = time;
            if (mode == Mode.FadeOut) {
                progressTime = 1f - progressTime;
            }

            material.Res.SetValue("progressTime", progressTime);

            ((DrawDevice)device).AddFullscreenQuad(device.RentMaterial(material), TargetResize.Fill);

            time += timeIncrement * Time.TimeMult;
            if (time >= 1f) {
                isCompleted = true;
            }
        }
    }
}