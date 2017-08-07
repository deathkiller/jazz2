using Duality;
using Duality.Resources;
using Jazz2.Game;
using Jazz2.Game.Structs;
using static Jazz2.Game.Tiles.TileMap;

namespace Jazz2.Actors.Weapons
{
    public class AmmoElectro : AmmoBase
    {
        public override WeaponType WeaponType => WeaponType.Electro;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            strength = 4;
            collisionFlags &= ~(CollisionFlags.ApplyGravitation | CollisionFlags.CollideWithTileset);
            collisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            RequestMetadata("Weapon/Electro");

            LightEmitter light = AddComponent<LightEmitter>();
            light.Intensity = 0.85f;
            light.Brightness = 0.8f;
            light.RadiusNear = 0f;
            light.RadiusFar = 20f;
        }

        public void OnFire(Player owner, Vector3 speed, float angle, bool isFacingLeft, byte upgrades)
        {
            base.owner = owner;
            base.isFacingLeft = isFacingLeft;
            base.upgrades = upgrades;

            float angleRel = angle * (isFacingLeft ? -1 : 1);

            float baseSpeed = ((upgrades & 0x1) != 0 ? 5f : 4f);
            if (isFacingLeft) {
                speedX = MathF.Min(0, speed.X) - MathF.Cos(angleRel) * baseSpeed;
            } else {
                speedX = MathF.Max(0, speed.X) + MathF.Cos(angleRel) * baseSpeed;
            }
            speedY = MathF.Sin(angleRel) * baseSpeed;
            speedY += MathF.Abs(speed.Y) * speedY;

            AnimState state = AnimState.Idle;
            if ((upgrades & 0x1) != 0) {
                timeLeft = 44;
                //state |= (AnimState)1;
            } else {
                timeLeft = 44;
            }

            Transform.Angle = angle;

            SetAnimation(state);
            PlaySound("Fire");
        }

        protected override void OnUpdate()
        {
            OnUpdateHitbox();
            CheckCollisions();
            TryStandardMovement();

            base.OnUpdate();

            for (int i = 0; i < 3; i++) {
                Material material = currentAnimation.Material.Res;
                Texture texture = material.MainTexture.Res;

                Vector3 pos = Transform.Pos;
                float dx = MathF.Rnd.NextFloat(-10f, 10f);
                float dy = MathF.Rnd.NextFloat(-10f, 10f);

                const float currentSize = 1.2f;
                int currentFrame = renderer.CurrentFrame;

                api.TileMap.CreateDebris(new DestructibleDebris {
                    Pos = new Vector3(pos.X + dx, pos.Y + dy, pos.Z),
                    Size = new Vector2(currentSize, currentSize),
                    Speed = new Vector2(0f, 0f),
                    Acceleration = new Vector2(0f, 0f),

                    Scale = 1f,
                    Alpha = 1f,
                    AlphaSpeed = MathF.Rnd.NextFloat(-0.05f, -0.02f),

                    Time = 240f,

                    Material = material,
                    MaterialOffset = new Rect(
                        (((float)(currentFrame % currentAnimation.Base.FrameConfiguration.X) / currentAnimation.Base.FrameConfiguration.X) + ((float)dx / texture.ContentWidth) + 0.5f) * texture.UVRatio.X,
                        (((float)(currentFrame / currentAnimation.Base.FrameConfiguration.X) / currentAnimation.Base.FrameConfiguration.Y) + ((float)dy / texture.ContentHeight) + 0.5f) * texture.UVRatio.Y,
                        (currentSize * texture.UVRatio.X / texture.ContentWidth),
                        (currentSize * texture.UVRatio.Y / texture.ContentHeight)
                    )
                });
            }
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(4, 4);
        }

        protected override bool OnPerish(ActorBase collider)
        {
            Explosion.Create(api, Transform.Pos + Speed, Explosion.SmokeGray);

            return base.OnPerish(collider);
        }

        protected override void OnHitFloorHook()
        {
            DecreaseHealth(int.MaxValue);
        }

        protected override void OnHitWallHook()
        {
            DecreaseHealth(int.MaxValue);
        }

        protected override void OnHitCeilingHook()
        {
            DecreaseHealth(int.MaxValue);
        }

        protected override void OnRicochet()
        {
        }
    }
}