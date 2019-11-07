using Duality;

namespace Jazz2.Actors.Solid
{
    public abstract class SolidObjectBase : ActorBase
    {
        public bool Movable { get; protected set; }
        public bool IsOneWay { get; protected set; }

        protected SolidObjectBase()
        {
            CollisionFlags |= CollisionFlags.CollideWithSolidObjects | CollisionFlags.IsSolidObject | CollisionFlags.SkipPerPixelCollisions;
        }

        public bool Push(bool left, float timeMult)
        {
            if (Movable) {
                for (int i = 0; i >= -4; i -= 2) {
                    if (MoveInstantly(new Vector2((left ? -0.5f : 0.5f) * timeMult, i * timeMult), MoveType.Relative)) {
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