using System;
using System.Threading.Tasks;
using Duality;
using Duality.Components;
using Duality.Drawing;
using Duality.Resources;
using Jazz2.Game;
using MathF = Duality.MathF;

namespace Jazz2.Actors
{
    partial class Player
    {
        public enum ShieldType : byte
        {
            None,

            Fire,
            Water,
            Lightning,
            Laser
        }

        private CircleEffectRenderer currentCircleEffectRenderer;

        private ActorBase shieldDecor, shieldComponentFront;
        private float shieldTime;

        private void SetCircleEffect(bool enabled)
        {
            if (enabled) {
                if (currentCircleEffectRenderer == null) {
                    currentCircleEffectRenderer = AddComponent<CircleEffectRenderer>();
                }
            } else {
                if (currentCircleEffectRenderer != null) {
                    RemoveComponent(currentCircleEffectRenderer);
                    currentCircleEffectRenderer = null;
                }
            }
        }

        public void SetShield(ShieldType shieldType, float secs)
        {
            if (shieldDecor != null) {
                Scene.RemoveObject(shieldDecor);
                shieldDecor = null;
            }

            if (shieldComponentFront != null) {
                Scene.RemoveObject(shieldComponentFront);
                shieldComponentFront = null;
            }

            if (shieldType == ShieldType.None) {
                shieldTime = 0f;
                return;
            }

            shieldTime = secs * Time.FramesPerSecond;

            switch (shieldType) {
                case ShieldType.Fire:
                    shieldDecor = new ShieldDecor(shieldType, false);
                    shieldDecor.OnActivated(new ActorActivationDetails {
                        Api = api
                    });
                    shieldDecor.Parent = this;

                    shieldComponentFront = new ShieldDecor(shieldType, true);
                    shieldComponentFront.OnActivated(new ActorActivationDetails {
                        Api = api
                    });
                    shieldComponentFront.Parent = this;
                    break;

                case ShieldType.Water:
                    shieldComponentFront = new ShieldDecor(shieldType, true);
                    shieldComponentFront.OnActivated(new ActorActivationDetails {
                        Api = api
                    });
                    shieldComponentFront.Parent = this;
                    break;

                case ShieldType.Lightning:
                    // ToDo
                    break;

                case ShieldType.Laser:
                    // ToDo
                    break;
            }
        }

        public bool IncreaseShieldTime(float secs)
        {
            if (shieldTime <= 0f) {
                return false;
            }

            shieldTime += secs * Time.FramesPerSecond;
            PlaySound("PickupGem");

            return true;
        }

        private class CircleEffectRenderer : Renderer
        {
            private struct CircleEffect
            {
                public Vector3 Pos;
                public float Radius;
                public float Alpha;
            }

            private CircleEffect[] circleEffectData = new CircleEffect[40];
            private int circleEffectIndex;
            private Material material;
            private float time;
            private VertexC1P3T2[] vertices = new VertexC1P3T2[32];

            public override float BoundRadius => 200;

            public CircleEffectRenderer()
            {
                material = new Material(ContentResolver.Current.RequestShader("BasicNormalAdd"));
                material.SetTexture("mainTex", Texture.White);
                material.SetTexture("normalTex", ContentResolver.Current.DefaultNormalMap);
                material.SetValue("normalMultiplier", Vector2.One);
                material.MainColor = new ColorRgba(60, 140, 255, 120);
            }

            public override void Draw(IDrawDevice device)
            {
                if (gameobj == null) {
                    return;
                }

                float timeMult = Time.TimeMult;

                for (int j = 0; j < circleEffectData.Length; j++) {
                    ref CircleEffect circle = ref circleEffectData[j];
                    if (circle.Alpha <= 0f) {
                        continue;
                    }

                    int segmentNum = MathF.Clamp(MathF.RoundToInt(MathF.Pow(circle.Radius, 0.65f) * 2.5f), 4, 32);

                    float angle = 0.0f;
                    for (int i = 0; i < segmentNum; i++) {
                        vertices[i].Pos.X = circle.Pos.X + (float)Math.Sin(angle) * circle.Radius;
                        vertices[i].Pos.Y = circle.Pos.Y - (float)Math.Cos(angle) * circle.Radius;
                        vertices[i].Pos.Z = circle.Pos.Z - 10f;
                        vertices[i].Color = new ColorRgba(1f, circle.Alpha);
                        angle += (MathF.TwoPi / segmentNum);
                    }

                    device.AddVertices(material, VertexMode.LineLoop, vertices, 0, segmentNum);

                    circle.Radius -= timeMult * 0.8f;
                    circle.Alpha -= timeMult * 0.03f;
                }

                circleEffectData[circleEffectIndex].Pos = gameobj.Transform.Pos;
                circleEffectData[circleEffectIndex].Radius = 32f + MathF.Sin(time * 0.1f) * 6f;
                circleEffectData[circleEffectIndex].Alpha = 1f;

                circleEffectIndex++;
                if (circleEffectIndex >= circleEffectData.Length) {
                    circleEffectIndex = 0;
                }

                time += timeMult;
            }
        }

        private class ShieldDecor : ActorBase
        {
            private readonly ShieldType shieldType;
            private readonly bool front;

            public ShieldDecor(ShieldType shieldType, bool front)
            {
                this.shieldType = shieldType;
                this.front = front;
            }

            protected override async Task OnActivatedAsync(ActorActivationDetails details)
            {
                await RequestMetadataAsync("Interactive/Shields");

                switch (shieldType)
                {
                    case ShieldType.Fire: SetAnimation(front ? "FireFront" : "Fire"); break;
                    case ShieldType.Water: SetAnimation("Water"); break;
                }
            }

            public override void OnFixedUpdate(float timeMult)
            {
                Transform.RelativePos = new Vector3(0f, 0f, front ? -2f : 2f);
            }
        }
    }
}