using Duality;
using Duality.Resources;
using Jazz2.Game;
using Jazz2.Game.Structs;
using static Jazz2.Game.Tiles.TileMap;

namespace Jazz2.Actors.Weapons
{
    public class AmmoElectro : AmmoBase
    {
        private LightEmitter light;

        public override WeaponType WeaponType => WeaponType.Electro;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            strength = 4;
            collisionFlags &= ~(CollisionFlags.ApplyGravitation | CollisionFlags.CollideWithTileset);
            collisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            RequestMetadata("Weapon/Electro");

            light = AddComponent<LightEmitter>();
            light.Intensity = 0.4f;
            light.Brightness = 0.2f;
            light.RadiusNear = 0f;
            light.RadiusFar = 12f;
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
                state |= (AnimState)1;
            } else {
                timeLeft = 44;
            }

            Transform.Angle = angle;
            Transform.Scale = 0.5f;

            SetAnimation(state);
            PlaySound("Fire");
        }

        protected override void OnUpdate()
        {
            OnUpdateHitbox();
            CheckCollisions();
            TryStandardMovement();

            base.OnUpdate();

            float timeMult = Time.TimeMult;

            Transform.Scale += 0.014f * timeMult;

            light.Intensity += 0.016f * timeMult;
            light.Brightness += 0.02f * timeMult;
            light.RadiusFar += 0.1f * timeMult;

            for (int i = 0; i < 5; i++) {
                Material material = currentAnimation.Material.Res;
                Texture texture = material.MainTexture.Res;

                Vector3 pos = Transform.Pos;
                float dx = MathF.Rnd.NextFloat(-10f, 10f);
                float dy = MathF.Rnd.NextFloat(-10f, 10f);

                float currentSizeX = MathF.Rnd.NextFloat(2f, 6f);
                float currentSizeY = 1f;
                int currentFrame = renderer.CurrentFrame;

                float sx = MathF.Rnd.NextFloat(-0.6f, 0.6f);
                float sy = MathF.Rnd.NextFloat(-0.6f, 0.6f);

                api.TileMap.CreateDebris(new DestructibleDebris {
                    Pos = new Vector3(pos.X + dx, pos.Y + dy, pos.Z),
                    Size = new Vector2(currentSizeX, currentSizeY),
                    Speed = new Vector2(sx, sy),
                    Acceleration = new Vector2(sx * 0.1f, sy * 0.1f),

                    Scale = 1f,
                    Alpha = 1f,
                    AlphaSpeed = MathF.Rnd.NextFloat(-0.05f, -0.02f),

                    Angle = MathF.Atan2(sy, sx),

                    Time = 240f,

                    Material = material,
                    MaterialOffset = new Rect(
                        (((float)(currentFrame % currentAnimation.Base.FrameConfiguration.X) / currentAnimation.Base.FrameConfiguration.X) + ((float)dx / texture.ContentWidth) + 0.5f) * texture.UVRatio.X,
                        (((float)(currentFrame / currentAnimation.Base.FrameConfiguration.X) / currentAnimation.Base.FrameConfiguration.Y) + ((float)dy / texture.ContentHeight) + 0.5f) * texture.UVRatio.Y,
                        (currentSizeX * texture.UVRatio.X / texture.ContentWidth),
                        (currentSizeY * texture.UVRatio.Y / texture.ContentHeight)
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