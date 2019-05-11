using Duality;
using Jazz2.Actors;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Game.Multiplayer
{
    public class RemotePlayer : ActorBase
    {
        public int Index;
        public PlayerType PlayerType;

        private Vector3 lastPos1, lastPos2;
        private double lastTime1, lastTime2;


        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            PlayerType = (PlayerType)details.Params[0];
            Index = details.Params[1];

            lastPos1 = lastPos2 = details.Pos;
            lastTime2 = Time.MainTimer.TotalMilliseconds;
            lastTime1 = lastTime2 - 1000;

            health = int.MaxValue;

            switch (PlayerType) {
                case PlayerType.Jazz:
                    RequestMetadata("Interactive/PlayerJazz");
                    break;
                case PlayerType.Spaz:
                    RequestMetadata("Interactive/PlayerSpaz");
                    break;
                case PlayerType.Lori:
                    RequestMetadata("Interactive/PlayerLori");
                    break;
                case PlayerType.Frog:
                    RequestMetadata("Interactive/PlayerFrog");
                    break;
            }

            SetAnimation(AnimState.Fall);

            collisionFlags = CollisionFlags.CollideWithOtherActors;
        }

        protected override void OnUpdate()
        {
            double time = Time.MainTimer.TotalMilliseconds;

            float alpha = MathF.Clamp((float)((time - lastTime1) / (lastTime2 - lastTime1)), 0f, 1f);

            Transform.Pos = lastPos1 + (lastPos2 - lastPos1) * alpha;

            base.OnUpdate();
        }

        public void UpdateFromServer(Vector3 pos, AnimState animState, float animTime, bool isFacingLeft)
        {
            lastPos1 = lastPos2;
            lastPos2 = pos;

            lastTime1 = lastTime2;
            lastTime2 = Time.MainTimer.TotalMilliseconds;

            if (availableAnimations != null) {
                if (currentAnimationState != animState) {
                    SetAnimation(animState);
                }

                if (animTime < 0) {
                    renderer.Active = false;
                } else {
                    renderer.Active = true;
                    renderer.AnimTime = animTime;
                    IsFacingLeft = isFacingLeft;
                }
            }
        }

        public override void OnHandleCollision(ActorBase other)
        {
            switch (other) {
                case AmmoBase ammo: {
                    api.BroadcastTriggeredEvent(EventType.ModifierHurt, new ushort[] { (ushort)Index, 1 });
                    ammo.DecreaseHealth(int.MaxValue);
                    break;
                }
            }
        }
    }
}