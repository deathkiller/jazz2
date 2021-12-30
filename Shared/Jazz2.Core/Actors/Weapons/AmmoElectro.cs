﻿using System.Threading.Tasks;
using Duality;
using Duality.Components;
using Duality.Drawing;
using Duality.Resources;
using Jazz2.Actors.Enemies;
using Jazz2.Game;
using Jazz2.Game.Components;
using Jazz2.Game.Structs;
using static Jazz2.Game.Tiles.TileMap;

namespace Jazz2.Actors.Weapons
{
    public partial class AmmoElectro : AmmoBase
    {
        private Vector2 gunspotPos;
        private bool fired;

        private LightEmitter light;

        private Material material1, material2;
        private float currentStep;

        public override WeaponType WeaponType => WeaponType.Electro;

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new AmmoElectro();
            actor.OnActivated(details);
            return actor;
        }

        public AmmoElectro()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await base.OnActivatedAsync(details);

            base.upgrades = (byte)details.Params[0];

            strength = 4;
            CollisionFlags &= ~(CollisionFlags.ApplyGravitation | CollisionFlags.CollideWithTileset);
            CollisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            await RequestMetadataAsync("Weapon/Electro");

            ColorRgba color1, color2;
            if ((upgrades & 0x1) != 0) {
                timeLeft = 55;
                color1 = new ColorRgba(160, 245, 255, 140);
                color2 = new ColorRgba(20, 170, 255, 140);
            } else {
                timeLeft = 55;
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

            light = AddComponent<LightEmitter>();
            light.Intensity = 0.4f;
            light.Brightness = 0.2f;
            light.RadiusNear = 0f;
            light.RadiusFar = 12f;
        }

        public void OnFire(Player owner, Vector3 gunspotPos, Vector3 speed, float angle, bool isFacingLeft)
        {
            base.owner = owner;
            base.IsFacingLeft = isFacingLeft;

            this.gunspotPos = gunspotPos.Xy;

            float angleRel = angle * (isFacingLeft ? -1 : 1);

            float baseSpeed = ((upgrades & 0x1) != 0 ? 5f : 4f);
            if (isFacingLeft) {
                speedX = MathF.Min(0, speed.X) - MathF.Cos(angleRel) * baseSpeed;
            } else {
                speedX = MathF.Max(0, speed.X) + MathF.Cos(angleRel) * baseSpeed;
            }
            speedY = MathF.Sin(angleRel) * baseSpeed;
        }

        public override void OnFixedUpdate(float timeMult)
        {
            float halfTimeMult = timeMult * 0.5f;

            for (int i = 0; i < 2; i++) {
                TryMovement(halfTimeMult);
                OnUpdateHitbox();
                CheckCollisions(halfTimeMult);
            }

            base.OnFixedUpdate(timeMult);

            // Adjust light
            light.Intensity += 0.016f * timeMult;
            light.Brightness += 0.02f * timeMult;
            light.RadiusFar += 0.1f * timeMult;

            if (!fired) {
                fired = true;

                MoveInstantly(gunspotPos, MoveType.Absolute, true);
            } else {
                // Spawn particles
                Vector3 pos = Transform.Pos;

                for (int i = 0; i < 6; i++) {
                    float angle = (currentStep * 0.3f + i * 0.6f);
                    if (IsFacingLeft) {
                        angle = -angle;
                    }

                    float size = (2f + currentStep * 0.1f);
                    float dist = (1f + currentStep * 0.01f);
                    float dx = dist * (float)System.Math.Cos(angle);
                    float dy = dist * (float)System.Math.Sin(angle);

                    levelHandler.TileMap.CreateDebris(new DestructibleDebris {
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
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(4, 4);
        }

        public override void OnHandleCollision(ActorBase other)
        {
            EnemyBase enemy = other as EnemyBase;
            if (enemy != null && (enemy.IsInvulnerable || !enemy.CanCollideWithAmmo)) {
                return;
            }

            base.OnHandleCollision(other);
        }

        protected override void OnHitWall()
        {
        }

        protected override void OnRicochet()
        {
        }
    }
}