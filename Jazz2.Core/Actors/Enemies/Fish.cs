using System.Collections.Generic;
using Duality;
using Duality.Resources;
using Jazz2.Game.Structs;
using static Jazz2.Game.Tiles.TileMap;

namespace Jazz2.Actors.Enemies
{
    public class Fish : EnemyBase
    {
        private const float DefaultSpeed = -2f;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            collisionFlags &= ~CollisionFlags.ApplyGravitation;

            SetHealthByDifficulty(1);
            scoreValue = 100;

            RequestMetadata("Enemy/Fish");
            SetAnimation(AnimState.Idle);

            isFacingLeft = MathF.Rnd.NextBool();
        }

        // TODO: Implement this

        protected override void OnUpdate()
        {
            base.OnUpdate();

            canJump = false;

            Vector3 pos = Transform.Pos;
            Vector3 targetPos;

            List<Player> players = api.Players;
            for (int i = 0; i < players.Count; i++) {
                targetPos = players[i].Transform.Pos;
                Vector3 direction = (pos - targetPos);
                float length = direction.Length;
                if (length < 180f) {
                    if (length > 120f) {
                        direction.Normalize();
                        speedX = direction.X * DefaultSpeed;
                        speedY = direction.Y * DefaultSpeed;

                        isFacingLeft = (speedX < 0f);
                    }
                    return;
                }
            }

            speedX = speedY = 0;
        }

        protected override bool OnPerish(ActorBase collider)
        {
            {
                const int debrisSize = 3;

                Vector3 pos = Transform.Pos;
                Material material = currentAnimation.Material.Res;
                Texture texture = material.MainTexture.Res;

                float x = pos.X - currentAnimation.Base.Hotspot.X;
                float y = pos.Y - currentAnimation.Base.Hotspot.Y;

                for (int fx = 0; fx < currentAnimation.Base.FrameDimensions.X; fx += debrisSize + 1) {
                    for (int fy = 0; fy < currentAnimation.Base.FrameDimensions.Y; fy += debrisSize + 1) {
                        float currentSize = debrisSize * MathF.Rnd.NextFloat(0.2f, 1.1f);
                        api.TileMap.CreateDebris(new DestructibleDebris {
                            Pos = new Vector3(x + (isFacingLeft ? currentAnimation.Base.FrameDimensions.X - fx : fx), y + fy, pos.Z),
                            Size = new Vector2(currentSize /** (isFacingLeft ? -1f : 1f)*/, currentSize),
                            Speed = new Vector2(((fx - currentAnimation.Base.FrameDimensions.X / 2) + MathF.Rnd.NextFloat(-2f, 2f)) * (isFacingLeft ? -1f : 1f) * MathF.Rnd.NextFloat(1f, 3f) / currentAnimation.Base.FrameDimensions.X,
                                 ((fy - currentAnimation.Base.FrameDimensions.Y / 2) + MathF.Rnd.NextFloat(-2f, 2f)) * (isFacingLeft ? -1f : 1f) * MathF.Rnd.NextFloat(1f, 3f) / currentAnimation.Base.FrameDimensions.Y),
                            Acceleration = new Vector2(0f, 0f),

                            Scale = 1f,
                            Alpha = 1f,
                            AlphaSpeed = -0.004f,

                            Time = 340f,

                            Material = material,
                            MaterialOffset = new Rect(
                                 (((float)(renderer.CurrentFrame % currentAnimation.Base.FrameConfiguration.X) / currentAnimation.Base.FrameConfiguration.X) + ((float)fx / texture.ContentWidth)) * texture.UVRatio.X,
                                 (((float)(renderer.CurrentFrame / currentAnimation.Base.FrameConfiguration.X) / currentAnimation.Base.FrameConfiguration.Y) + ((float)fy / texture.ContentHeight)) * texture.UVRatio.Y,
                                 (currentSize * texture.UVRatio.X / texture.ContentWidth),
                                 (currentSize * texture.UVRatio.Y / texture.ContentHeight)
                             ),

                            CollisionAction = DebrisCollisionAction.Disappear
                        });
                    }
                }
            }

            api.PlayCommonSound(this, "Splat");

            Explosion.Create(api, Transform.Pos, Explosion.SmallDark);

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}