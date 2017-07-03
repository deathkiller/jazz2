using Duality;

namespace Jazz2.Actors.Solid
{
    public abstract class SolidObjectBase : ActorBase
    {
        public bool Movable { get; protected set; }
        public bool IsOneWay { get; protected set; }

        public SolidObjectBase()
        {
            collisionFlags |= CollisionFlags.CollideWithSolidObjects;
            collisionFlags |= CollisionFlags.IsSolidObject;
        }

        public bool Push(bool left)
        {
            if (Movable) {
                return MoveInstantly(new Vector2(left ? -0.7f : 0.7f, 0f), MoveType.RelativeTime);
            } else {
                return false;
            }
        }
    }
}