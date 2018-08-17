using Duality;
using Jazz2.Actors;
using Jazz2.Game.Structs;
using Jazz2.Networking.Packets.Server;

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
            Vector3 pos = p.Pos;
            pos.X += p.Speed.X * p.SenderConnection.AverageRoundtripTime * 0.5f;
            pos.Y += p.Speed.Y * p.SenderConnection.AverageRoundtripTime * 0.5f;

            Transform.Pos = pos;

            speedX = p.Speed.X;
            speedY = p.Speed.Y;

            if (currentAnimationState != p.AnimState) {
                SetAnimation(p.AnimState);
            }

            renderer.AnimTime = p.AnimTime;
            IsFacingLeft = p.IsFacingLeft;
        }
    }
}