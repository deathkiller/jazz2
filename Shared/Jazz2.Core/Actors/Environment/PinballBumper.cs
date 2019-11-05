using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Components;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Environment
{
    public class PinballBumper : ActorBase
    {
        private float cooldown;
        private LightEmitter light;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            ushort theme = details.Params[0];

            collisionFlags = CollisionFlags.CollideWithOtherActors;

            await RequestMetadataAsync("Object/PinballBumper");

            switch (theme) {
                case 0: SetAnimation((AnimState)0x000); break;
                case 1: SetAnimation((AnimState)0x001); break;
            }

            light = AddComponent<LightEmitter>();
            light.RadiusNear = 24f;
            light.RadiusFar = 60f;
        }

        public override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            if (cooldown > 0f) {
                cooldown -= timeMult;
            }

            if (light.Intensity > 0f) {
                light.Intensity -= timeMult * 0.01f;

                if (light.Intensity < 0f) {
                    light.Intensity = 0f;
                }
            }

            if (light.Brightness > 0f) {
                light.Brightness -= timeMult * 0.02f;

                if (light.Brightness < 0f) {
                    light.Brightness = 0f;
                }
            }
        }

        public Vector2 Activate(ActorBase other)
        {
            if (cooldown > 0f) {
                return Vector2.Zero;
            }

            cooldown = 10f;
            light.Intensity = 1f;
            light.Brightness = 0.4f;

            SetTransition(currentAnimationState | (AnimState)0x200, true);
            PlaySound("Hit", 0.8f);

            const float force = 24f;

            Vector3 pos = other.Transform.Pos - Transform.Pos;
            return pos.Xy.Normalized * force;
        }
    }
}