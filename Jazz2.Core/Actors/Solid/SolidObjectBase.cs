using Duality;

namespace Jazz2.Actors.Solid
{
    public abstract class SolidObjectBase : ActorBase
    {
        public bool Movable { get; protected set; }
        public bool IsOneWay { get; protected set; }

        public SolidObjectBase()
        {
            collisionFlags |= CollisionFlags.CollideWithSolidObjects | CollisionFlags.IsSolidObject;
        }

        public bool Push(bool left)
        {
            if (Movable) {
                for (int i = 0; i >= -4; i -= 2) {
                    if (MoveInstantly(new Vector2(left ? -0.5f : 0.5f, i), MoveType.RelativeTime)) {
                        return true;
                    }
                }

                return false;
            } else {
                return false;
            }
        }
    }
}