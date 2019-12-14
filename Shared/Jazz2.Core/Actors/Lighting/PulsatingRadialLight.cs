using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Components;

namespace Jazz2.Actors.Lighting
{
    public class PulsatingRadialLight : ActorBase
    {
        private const float BaseCycleFrames = 700f;

        private LightEmitter light;
        private float radiusNear1, radiusNear2, radiusFar;
        private float phase, speed;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            ushort intensity = details.Params[0];
            ushort brightness = details.Params[1];
            this.radiusNear1 = details.Params[2];
            this.radiusNear2 = details.Params[3];
            this.radiusFar = details.Params[4];
            this.speed = details.Params[5] * 0.6f;
            ushort sync = details.Params[6];

            CollisionFlags = CollisionFlags.ForceDisableCollisions;

            phase = (BaseCycleFrames - ((float)(Time.GameTimer.TotalMilliseconds % BaseCycleFrames) + sync * 175)) % BaseCycleFrames;

            light = AddComponent<LightEmitter>();
            light.Intensity = (intensity / 255f);
            light.Brightness = (brightness / 255f);
        }

        public override void OnUpdate()
        {
            phase = (phase + speed * Time.TimeMult) % BaseCycleFrames;

            light.RadiusNear = radiusNear1 + MathF.Sin(MathF.TwoPi * phase / BaseCycleFrames) * radiusNear2;
            light.RadiusFar = light.RadiusNear + radiusFar;
        }

        public override void OnFixedUpdate(float timeMult)
        {
        }
    }
}