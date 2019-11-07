using System.Threading.Tasks;
using Jazz2.Game.Components;

namespace Jazz2.Actors.Lighting
{
    public class FlickerLight : ActorBase
    {
        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            ushort intensity = details.Params[0];
            ushort brightness = details.Params[1];
            ushort radiusNear = details.Params[2];
            ushort radiusFar = details.Params[3];

            CollisionFlags = CollisionFlags.ForceDisableCollisions;

            LightEmitter light = AddComponent<LightEmitter>();
            light.Intensity = (intensity / 255f);
            light.Brightness = (brightness / 255f);
            light.RadiusNear = radiusNear;
            light.RadiusFar = radiusFar;
            light.Type = LightType.WithNoise;
        }

        public override void OnFixedUpdate(float timeMult)
        {
        }
    }
}