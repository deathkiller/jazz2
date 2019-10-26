using System.Threading.Tasks;
using Duality;
using Duality.Resources;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;

namespace Jazz2.Actors.Environment
{
    public class AmbientBubbles : ActorBase
    {
        private ushort speed;
        private float cooldown;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            speed = details.Params[0];

            collisionFlags = CollisionFlags.ForceDisableCollisions;

            await RequestMetadataAsync("Common/AmbientBubbles");
        }

        public override void OnFixedUpdate(float timeMult)
        {
            if (cooldown > 0f) {
                cooldown -= timeMult;
            } else {
                GraphicResource res = availableAnimations["AmbientBubbles"];
                Material material = res.Material.Res;
                Texture texture = material.MainTexture.Res;

                for (int i = 0; i < speed; i++) {
                    float scale = MathF.Rnd.NextFloat(0.3f, 1.0f);
                    float speedX = MathF.Rnd.NextFloat(-0.5f, 0.5f) * scale;
                    float speedY = MathF.Rnd.NextFloat(-3f, -2f) * scale;
                    float accel = MathF.Rnd.NextFloat(-0.008f, -0.001f) * scale;

                    api.TileMap.CreateDebris(new TileMap.DestructibleDebris {
                        Pos = Transform.Pos,
                        Size = res.Base.FrameDimensions,
                        Speed = new Vector2(speedX, speedY),
                        Acceleration = new Vector2(0f, accel),

                        Scale = scale,
                        Alpha = 1f,
                        AlphaSpeed = -0.009f,

                        Time = 110f,

                        Material = material,
                        MaterialOffset = texture.LookupAtlas(res.FrameOffset + MathF.Rnd.Next(res.FrameCount)),

                        CollisionAction = TileMap.DebrisCollisionAction.None
                    });
                }

                cooldown = 20f;
            }
        }
    }
}