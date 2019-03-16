using Duality;
using Duality.Components;
using Duality.Drawing;
using Duality.Resources;
using Jazz2.Game;
using Jazz2.Game.Structs;
using static Jazz2.Game.Tiles.TileMap;

namespace Jazz2.Actors.Weapons
{
    public class AmmoElectro : AmmoBase
    {
        private LightEmitter light;

        private Material material1, material2;
        private float currentStep;

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
            base.IsFacingLeft = isFacingLeft;
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

            ColorRgba color1, color2;
            if ((upgrades & 0x1) != 0) {
                timeLeft = 44;
                color1 = new ColorRgba(160, 245, 255, 140);
                color2 = new ColorRgba(20, 170, 255, 140);
            } else {
                timeLeft = 44;
                color1 = new ColorRgba(255, 235, 20, 140);
                color2 = new ColorRgba(255, 120, 10, 140);
            }

            SetAnimation(AnimState.Idle);
            PlaySound("Fire");

            // Turn off default renderer
            renderer.Active = false;

            // Create materials for particles
            material1 = new Material(ContentResolver.Current.RequestShader("BasicNormal"));
            material1.SetTexture("mainTex", Texture.White);
            material1.SetTexture("normalTex", ContentResolver.Current.DefaultNormalMap);
            material1.SetValue("normalMultiplier", Vector2.One);
            material1.MainColor = color1;

            material2 = new Material(ContentResolver.Current.RequestShader("BasicNormal"));
            material2.SetTexture("mainTex", Texture.White);
            material2.SetTexture("normalTex", ContentResolver.Current.DefaultNormalMap);
            material2.SetValue("normalMultiplier", Vector2.One);
            material2.MainColor = color2;
        }

        protected override void OnUpdate()
        {
            OnUpdateHitbox();
            CheckCollisions();
            TryStandardMovement(Time.TimeMult);

            base.OnUpdate();

            float timeMult = Time.TimeMult;

            // Adjust light
            light.Intensity += 0.016f * timeMult;
            light.Brightness += 0.02f * timeMult;
            light.RadiusFar += 0.1f * timeMult;

            // Spawn particles
            Vector3 pos = Transform.Pos;

            for (int i = 0; i < 6; i++) {
                float angle = (currentStep * 0.3f + i * 0.6f);
                float size = (2f + currentStep * 0.1f);
                float dist = (1f + currentStep * 0.01f);
                float dx = dist * (float)System.Math.Cos(angle);
                float dy = dist * (float)System.Math.Sin(angle);

                api.TileMap.CreateDebris(new DestructibleDebris {
                    Pos = new Vector3(pos.X + dx, pos.Y + dy, pos.Z),
                    Size = new Vector2(size, size),

                    Scale = 1f,
                    ScaleSpeed = -0.1f,
                    Alpha = 1f,
                    AlphaSpeed = -0.2f,

                    Angle = angle,

                    Time = 60f,

                    Material = (MathF.Rnd.NextFloat() < 0.6f ? material1 : material2),
                    MaterialOffset = new Rect(0, 0, 1, 1)
                });
            }

            currentStep += timeMult;
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