using System.Threading.Tasks;
using Duality;
using Duality.Components;
using Jazz2.Game;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Lighting
{
    public class IlluminateLight : ActorBase
    {
        public override EventType EventType => EventType.LightIlluminate;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            ushort size = details.Params[0];

            collisionFlags = CollisionFlags.ForceDisableCollisions;

            const int lightCount = 8;

            for (int i = 0; i < lightCount; i++) {
                new IlluminateLightPart(size).Parent = this;
            }
        }

        protected override void OnFixedUpdate(float timeMult)
        {
        }
    }

    // Used in ActorBase.Illuminate(), cannot be used on its own
    public class IlluminateLightPart : GameObject
    {
        private float distance, phase, speed;

        public IlluminateLightPart(float size)
        {
            float intensity = MathF.Rnd.NextFloat(0.22f, 0.42f);

            distance = MathF.Rnd.NextFloat(6f, 46f) * size;
            phase = MathF.Rnd.NextFloat(MathF.TwoPi);
            speed = MathF.Rnd.NextFloat(-0.18f, -0.06f);

            AddComponent<Transform>();
            AddComponent(new LocalController(this));

            LightEmitter light = AddComponent<LightEmitter>();
            light.Intensity = intensity * 0.7f;
            light.Brightness = intensity;
            light.RadiusFar = intensity * 60f * size;
            light.Type = LightType.Solid;
        }

        public void UpdatePosition()
        {
            Transform.RelativePos = new Vector3(MathF.Cos(phase + MathF.Cos(phase * 0.33f) * 0.33f) * distance, MathF.Sin(phase + MathF.Sin(phase) * 0.33f) * distance, 0f);

            phase += speed * Time.TimeMult;
        }

        private class LocalController : Component, ICmpUpdatable
        {
            private readonly IlluminateLightPart light;

            public LocalController(IlluminateLightPart light)
            {
                this.light = light;
            }

            void ICmpUpdatable.OnUpdate()
            {
                light.UpdatePosition();
            }
        }
    }
}