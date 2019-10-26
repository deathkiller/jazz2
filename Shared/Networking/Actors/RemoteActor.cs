using Duality;
using Duality.Components;
using Jazz2.Actors;
using Jazz2.Game.Structs;

namespace Jazz2.Game.Multiplayer
{
    public class RemoteActor : ActorBase
    {
        public async void OnActivated(ILevelHandler levelHandler, Vector3 pos, string metadataPath)
        {
            initState = InitState.Initializing;

            this.levelHandler = levelHandler;
            this.flags = ActorInstantiationFlags.None;

            friction = 1.5f;

            originTile = new Point2((int)(pos.X / 32), (int)(pos.Y / 32));

            Transform transform = AddComponent<Transform>();
            transform.Pos = pos;

            //AddComponent(new LocalController(this));

            //await OnActivatedAsync(details);

            await RequestMetadataAsync(metadataPath);

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

        public void OnRefreshActorAnimation(string identifier)
        {
            SetAnimation(identifier);

            OnUpdateHitbox();
        }
    }
}