using System.Threading.Tasks;
using Duality;
using Jazz2.Actors;
using Jazz2.Game.Structs;

namespace Jazz2.Game.Multiplayer
{
    public class RemoteObject : ActorBase
    {
        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            health = int.MaxValue;

            // ToDo: Load metadata
            await RequestMetadataAsync("Weapon/Blaster");

            SetAnimation(AnimState.Fall);

            collisionFlags = CollisionFlags.CollideWithOtherActors;

            LightEmitter light = AddComponent<LightEmitter>();
            light.Intensity = 0.8f;
            light.Brightness = 0.6f;
            light.RadiusNear = 5f;
            light.RadiusFar = 20f;
        }

        public void UpdateFromServer(Vector3 pos, Vector2 speed, AnimState animState, float animTime, bool isFacingLeft)
        {
            Transform.Pos = pos;

            speedX = speed.X;
            speedY = speed.Y;

            if (availableAnimations != null) {
                if (currentAnimationState != animState) {
                    SetAnimation(animState);
                }

                renderer.AnimTime = animTime;
                IsFacingLeft = isFacingLeft;
            }
        }
    }
}