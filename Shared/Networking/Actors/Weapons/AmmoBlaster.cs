using Duality;
using Jazz2.Game.Structs;
using Jazz2.Networking;
using Jazz2.Networking.Packets.Client;

namespace Jazz2.Actors.Weapons
{
    partial class AmmoBlaster : IRemotableActor
    {
        public int Index { get; set; }

        public void OnCreateRemotableActor(ref CreateRemotableActor p)
        {
            p.EventType = EventType.WeaponBlaster;
            p.EventParams = new ushort[] { upgrades };
            p.Pos = Transform.Pos;
        }

        public void OnUpdateRemotableActor(ref UpdateRemotableActor p)
        {
            p.Pos = Transform.Pos;
            p.Speed = Speed.Xy;
            p.AnimState = (currentTransitionState != AnimState.Idle ? currentTransitionState : currentAnimationState);
            p.AnimTime = (renderer.Active ? renderer.AnimTime : -1);
            p.IsFacingLeft = IsFacingLeft;
        }

        public void OnUpdateRemoteActor(Vector3 pos, Vector2 speed, AnimState animState, float animTime, bool isFacingLeft)
        {
            Transform.Pos = pos;

            speedX = speed.X;
            speedY = speed.Y;

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

            collisionFlags = CollisionFlags.None;
        }
    }
}