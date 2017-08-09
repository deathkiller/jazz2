using Duality;

namespace Jazz2
{
    public class Ease
    {
        public static float OutElastic(float t)
        {
            const float p = 0.3f;
            return MathF.Pow(2, -10 * t) * MathF.Sin((t - p / 4) * (2 * MathF.Pi) / p) + 1;
        }
    }
}