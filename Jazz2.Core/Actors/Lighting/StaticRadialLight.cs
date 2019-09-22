using System.Threading.Tasks;
using Jazz2.Game;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Lighting
{
    public class StaticRadialLight : ActorBase
    {
        public override EventType EventType => EventType.LightSteady;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            ushort intensity = details.Params[0];
            ushort brightness = details.Params[1];
            ushort radiusNear = details.Params[2];
            ushort radiusFar = details.Params[3];

            collisionFlags = CollisionFlags.ForceDisableCollisions;

            LightEmitter light = AddComponent<LightEmitter>();
            light.Intensity = (intensity / 255f);
            light.Brightness = (brightness / 255f);
            light.RadiusNear = radiusNear;
            light.RadiusFar = radiusFar;
        }

        protected override void OnFixedUpdate(float timeMult)
        {
        }
    }
}