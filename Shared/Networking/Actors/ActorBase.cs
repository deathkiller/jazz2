using Duality;

namespace Jazz2.Actors
{
    partial class ActorBase
    {
        public int Index { get; set; }

        public void OnUpdateRemoteActor(Vector3 pos, Vector2 speed, bool visible, bool isFacingLeft)
        {
            Transform.Pos = pos;

            speedX = speed.X;
            speedY = speed.Y;

            if (availableAnimations != null && renderer != null) {
                renderer.Active = visible;
                IsFacingLeft = isFacingLeft;
            }

            collisionFlags = CollisionFlags.None;
        }
    }
}
