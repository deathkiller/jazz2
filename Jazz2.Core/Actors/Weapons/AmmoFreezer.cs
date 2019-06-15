using System.Threading.Tasks;
using Duality;
using Duality.Resources;
using Jazz2.Game;
using Jazz2.Game.Structs;
using static Jazz2.Game.Tiles.TileMap;

namespace Jazz2.Actors.Weapons
{
    public partial class AmmoFreezer : AmmoBase
    {
        private Vector2 gunspotPos;
        private bool fired;

        public override WeaponType WeaponType => WeaponType.Freezer;

        public float FrozenDuration => ((upgrades & 0x1) != 0 ? 280f : 180f);

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await base.OnActivatedAsync(details);

            base.upgrades = (byte)details.Params[0];

            collisionFlags &= ~CollisionFlags.ApplyGravitation;
            strength = 0;

            await RequestMetadataAsync("Weapon/Freezer");

            AnimState state = AnimState.Idle;

            if ((upgrades & 0x1) != 0) {
                timeLeft = 38;
                state |= (AnimState)1;
                PlaySound("FireUpgraded");
            } else {
                timeLeft = 44;

                PlaySound("Fire");
            }

            SetAnimation(state);

            renderer.Active = false;

            LightEmitter light = AddComponent<LightEmitter>();
            light.Intensity = 0.8f;
            light.Brightness = 0.2f;
            light.RadiusNear = 0f;
            light.RadiusFar = 20f;
        }

        public void OnFire(Player owner, Vector3 gunspotPos, Vector3 speed, float angle, bool isFacingLeft)
        {
            base.owner = owner;
            base.IsFacingLeft = isFacingLeft;

            this.gunspotPos = gunspotPos.Xy;

            float angleRel = angle * (isFacingLeft ? -1 : 1);

            float baseSpeed = ((upgrades & 0x1) != 0 ? 8f : 6f);
            if (isFacingLeft) {
                speedX = MathF.Min(0, speed.X) - MathF.Cos(angleRel) * baseSpeed;
            } else {
                speedX = MathF.Max(0, speed.X) + MathF.Cos(angleRel) * baseSpeed;
            }
            speedY = MathF.Sin(angleRel) * baseSpeed;

            Transform.Angle = angle;
        }

        protected override void OnUpdate()
        {
            float timeMult = Time.TimeMult * 0.5f;

            for (int i = 0; i < 2; i++) {
                TryMovement(timeMult);
                OnUpdateHitbox();
                CheckCollisions(timeMult);
            }

            base.OnUpdate();

            Material material = currentAnimation.Material.Res;
            Texture texture = material.MainTexture.Res;
            if (texture != null) {
                Vector3 pos = Transform.Pos;
                if (pos.Y < api.WaterLevel) {
                    float dx = MathF.Rnd.NextFloat(-8f, 8f);
                    float dy = MathF.Rnd.NextFloat(-3f, 3f);

                    const float currentSize = 1f;
                    int currentFrame = renderer.CurrentFrame;

                    api.TileMap.CreateDebris(new DestructibleDebris {
                        Pos = new Vector3(pos.X + dx, pos.Y + dy, pos.Z + 1f),
                        Size = new Vector2(currentSize, currentSize),
                        Acceleration = new Vector2(0f, api.Gravity),

                        Scale = 1.2f,
                        Alpha = 1f,

                        Time = 300f,

                        Material = material,
                        MaterialOffset = new Rect(
                            (((float)(currentFrame % currentAnimation.Base.FrameConfiguration.X) / currentAnimation.Base.FrameConfiguration.X) + ((float)dx / texture.ContentWidth) + 0.5f) * texture.UVRatio.X,
                            (((float)(currentFrame / currentAnimation.Base.FrameConfiguration.X) / currentAnimation.Base.FrameConfiguration.Y) + ((float)dy / texture.ContentHeight) + 0.5f) * texture.UVRatio.Y,
                            (currentSize * texture.UVRatio.X / texture.ContentWidth),
                            (currentSize * texture.UVRatio.Y / texture.ContentHeight)
                        ),

                        CollisionAction = DebrisCollisionAction.Disappear
                    });
                }
            }

            if (timeLeft <= 0f) {
                PlaySound("WallPoof");
            }

            if (!fired) {
                fired = true;

                MoveInstantly(gunspotPos, MoveType.Absolute, true);
                renderer.Active = true;
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            Explosion.Create(api, Transform.Pos + Speed, Explosion.SmokeWhite);

            return base.OnPerish(collider);
        }

        protected override void OnHitWall()
        {
            DecreaseHealth(int.MaxValue);

            PlaySound("WallPoof");
        }
    }
}