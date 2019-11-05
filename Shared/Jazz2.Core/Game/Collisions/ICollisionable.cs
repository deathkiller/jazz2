namespace Jazz2.Game.Collisions
{
    public interface ICollisionable
    {
        ref int ProxyId { get; }
        ref AABB AABB { get; }
    }
}
