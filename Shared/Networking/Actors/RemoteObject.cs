using System.Threading.Tasks;
using Duality;
using Duality.Components;
using Jazz2.Actors;
using Jazz2.Game.Structs;

namespace Jazz2.Game.Multiplayer
{
    public class RemoteObject : ActorBase
    {
        /*protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            health = int.MaxValue;*/

            // ToDo: Load metadata
            /*await RequestMetadataAsync("Weapon/Blaster");

            SetAnimation(AnimState.Fall);

            collisionFlags = CollisionFlags.CollideWithOtherActors;

            LightEmitter light = AddComponent<LightEmitter>();
            light.Intensity = 0.8f;
            light.Brightness = 0.6f;
            light.RadiusNear = 5f;
            light.RadiusFar = 20f;*/
        //}


        public async void OnActivated(ActorApi api, Vector3 pos, string metadataPath, AnimState animState)
        {
            initState = InitState.Initializing;

            this.api = api;
            this.flags = ActorInstantiationFlags.None;

            friction = 1.5f;

            originTile = new Point2((int)(pos.X / 32), (int)(pos.Y / 32));

            Transform transform = AddComponent<Transform>();
            transform.Pos = pos;

            //AddComponent(new LocalController(this));

            //await OnActivatedAsync(details);

            await RequestMetadataAsync(metadataPath);

            SetAnimation(animState);

            OnUpdateHitbox();

            if (initState == InitState.Initializing) {
                initState = InitState.Initialized;
            }
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