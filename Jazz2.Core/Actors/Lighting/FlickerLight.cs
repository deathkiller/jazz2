using Jazz2.Game;

namespace Jazz2.Actors.Lighting
{
    public class FlickerLight : ActorBase
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            ushort alpha = details.Params[0];
            ushort radiusNear = details.Params[1];
            ushort radiusFar = details.Params[2];

            collisionFlags = CollisionFlags.None;

            LightEmitter light = AddComponent<LightEmitter>();
            light.Intensity = (alpha / 255f);
            light.RadiusNear = radiusNear;
            light.RadiusFar = radiusFar;
            light.Type = LightType.WithNoise;
        }

        protected override void OnUpdate()
        {
        }
    }
}