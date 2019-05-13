using Jazz2.Game;

namespace Jazz2.Actors.Lighting
{
    public class FlickerLight : ActorBase
    {
        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

            ushort intensity = details.Params[0];
            ushort brightness = details.Params[1];
            ushort radiusNear = details.Params[2];
            ushort radiusFar = details.Params[3];

            collisionFlags = CollisionFlags.None;

            LightEmitter light = AddComponent<LightEmitter>();
            light.Intensity = (intensity / 255f);
            light.Brightness = (brightness / 255f);
            light.RadiusNear = radiusNear;
            light.RadiusFar = radiusFar;
            light.Type = LightType.WithNoise;
        }

        protected override void OnUpdate()
        {
        }
    }
}