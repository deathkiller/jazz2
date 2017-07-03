using Duality;

namespace Jazz2.Game.Structs
{
    public struct Hitbox
    {
        public float Left;
        public float Top;
        public float Right;
        public float Bottom;

        public Hitbox(float left, float top, float right, float bottom)
        {
            this.Left = left;
            this.Top = top;
            this.Right = right;
            this.Bottom = bottom;
        }

        public bool Overlaps(ref Hitbox other) {
            return Left < other.Right && Right > other.Left
                && Top < other.Bottom && Bottom > other.Top;
        }

        public Hitbox Extend(float left, float top, float right, float bottom)
        {
            return new Hitbox(Left - left, Top - top, Right + right, Bottom + bottom);
        }

        public Hitbox Extend(float x, float y)
        {
            return new Hitbox(Left - x, Top - y, Right + x, Bottom + y);
        }

        public Hitbox Extend(float v)
        {
            return new Hitbox(Left - v, Top - v, Right + v, Bottom + v);
        }

        public static Hitbox operator +(Hitbox hitbox, Vector2 cp)
        {
            return new Hitbox(
                hitbox.Left + cp.X,
                hitbox.Top + cp.Y,
                hitbox.Right + cp.X,
                hitbox.Bottom + cp.Y
            );
        }

        public static Hitbox operator -(Hitbox hitbox, Vector2 cp)
        {
            return new Hitbox(
                hitbox.Left - cp.X,
                hitbox.Top - cp.Y,
                hitbox.Right - cp.X,
                hitbox.Bottom - cp.Y
            );
        }
    }
}