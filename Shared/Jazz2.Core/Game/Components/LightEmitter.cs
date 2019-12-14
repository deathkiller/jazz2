using Duality;

namespace Jazz2.Game.Components
{
    public enum LightType
    {
        Solid,
        WithNoise
    }

    public class LightEmitter : Component
    {
        public float Intensity;
        public float Brightness;
        public float RadiusNear;
        public float RadiusFar;
        public LightType Type;
    }
}