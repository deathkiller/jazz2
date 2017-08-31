#if MULTIPLAYER

using Jazz2.Actors;
using Jazz2.Game.Structs;
using Jazz2.NetworkPackets.Server;

namespace Jazz2.Game.Multiplayer
{
    public class RemotePlayer : ActorBase
    {
        private PlayerType playerType;
        private int index;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            playerType = (PlayerType)details.Params[0];
            index = details.Params[1];

            switch (playerType) {
                case PlayerType.Jazz:
                    RequestMetadata("Interactive/PlayerJazz");
                    break;
                case PlayerType.Spaz:
                    RequestMetadata("Interactive/PlayerSpaz");
                    break;
                case PlayerType.Lori:
                    RequestMetadata("Interactive/PlayerLori");
                    break;
            }

            SetAnimation(AnimState.Fall);

            //collisionFlags = CollisionFlags.CollideWithTileset | CollisionFlags.CollideWithSolidObjects | CollisionFlags.CollideWithOtherActors | CollisionFlags.ApplyGravitation | CollisionFlags.IsSolidObject;
            collisionFlags = CollisionFlags.CollideWithOtherActors;
        }

        public void UpdateFromServer(ref UpdateRemotePlayer p)
        {
            Transform.Pos = p.Pos;

            speedX = p.Speed.X;
            speedY = p.Speed.Y;

            if (currentAnimationState != p.AnimState) {
                SetAnimation(p.AnimState);
            }

            renderer.AnimTime = p.AnimTime;
            isFacingLeft = p.IsFacingLeft;
        }
    }
}

#endif