using Duality;
using Jazz2.Game;

namespace Jazz2.Actors.Lighting
{
    public class PulsatingRadialLight : ActorBase
    {
        private const float BaseCycleFrames = 700f;

        private LightEmitter light;
        private float radiusNear1, radiusNear2, radiusFar;
        private float phase, speed;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            ushort alpha = details.Params[0];
            ushort radiusNear1 = details.Params[1];
            ushort radiusNear2 = details.Params[2];
            ushort radiusFar = details.Params[3];
            ushort speed = details.Params[4];
            ushort sync = details.Params[5];

            collisionFlags = CollisionFlags.None;

            this.radiusNear1 = radiusNear1;
            this.radiusNear2 = radiusNear2;
            this.radiusFar = radiusFar;
            this.speed = 0.6f * speed;

            phase = (BaseCycleFrames - ((float)(Time.GameTimer.TotalMilliseconds % BaseCycleFrames) + sync * 175)) % BaseCycleFrames;

            light = AddComponent<LightEmitter>();
            light.Intensity = (alpha / 255f);
        }

        protected override void OnUpdate()
        {
            phase = (phase + speed * Time.TimeMult) % BaseCycleFrames;

            light.RadiusNear = radiusNear1 + MathF.Sin(MathF.TwoPi * phase / BaseCycleFrames) * radiusNear2;
            light.RadiusFar = light.RadiusNear + radiusFar;
        }
    }
}