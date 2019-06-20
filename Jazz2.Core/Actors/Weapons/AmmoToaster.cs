using System.Threading.Tasks;
using Duality;
using Jazz2.Game;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;

namespace Jazz2.Actors.Weapons
{
    public partial class AmmoToaster : AmmoBase
    {
        private Vector2 gunspotPos;
        private bool fired;

        public override WeaponType WeaponType => WeaponType.Toaster;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await base.OnActivatedAsync(details);

            base.upgrades = (byte)details.Params[0];

            collisionFlags &= ~CollisionFlags.ApplyGravitation;

            strength = 1;

            await RequestMetadataAsync("Weapon/Toaster");

            AnimState state = AnimState.Idle;
            if ((upgrades & 0x1) != 0) {
                timeLeft = 80;
                state |= (AnimState)1;
            } else {
                timeLeft = 60;
            }

            SetAnimation(state);

            renderer.Active = false;

            LightEmitter light = AddComponent<LightEmitter>();
            light.Intensity = 0.85f;
            light.Brightness = 0.4f;
            light.RadiusNear = 0f;
            light.RadiusFar = 30f;
        }

        public void OnFire(Player owner, Vector3 gunspotPos, Vector3 speed, float angle, bool isFacingLeft)
        {
            base.owner = owner;
            base.IsFacingLeft = isFacingLeft;

            this.gunspotPos = gunspotPos.Xy;

            float ax = MathF.Cos(angle);
            float ay = MathF.Sin(angle);

            const float baseSpeed = 1.2f;
            if (isFacingLeft) {
                speedX = MathF.Min(0, speed.X) - ax * (baseSpeed + MathF.Rnd.NextFloat(0f, 0.2f));
            } else {
                speedX = MathF.Max(0, speed.X) + ax * (baseSpeed + MathF.Rnd.NextFloat(0f, 0.2f));
            }
            speedX += ay * MathF.Rnd.NextFloat(-0.5f, 0.5f);

            if (isFacingLeft) {
                speedY = -ay * (baseSpeed + MathF.Rnd.NextFloat(0f, 0.2f));
            } else {
                speedY = ay * (baseSpeed + MathF.Rnd.NextFloat(0f, 0.2f));
            }
            speedY += ax * MathF.Rnd.NextFloat(-0.5f, 0.5f);
        }

        protected override void OnFixedUpdate(float timeMult)
        {
            OnUpdateHitbox();

            Vector3 pos = Transform.Pos;

            if (pos.Y >= api.WaterLevel) {
                DecreaseHealth(int.MaxValue);
                return;
            }

            TileMap tiles = api.TileMap;
            if (tiles == null || tiles.IsTileEmpty(ref AABBInner, false)) {
                MoveInstantly(new Vector2(speedX * timeMult, speedY * timeMult), MoveType.Relative, true);
                CheckCollisions(timeMult);
            } else {
                MoveInstantly(new Vector2(speedX * timeMult, speedY * timeMult), MoveType.Relative, true);
                CheckCollisions(timeMult);
                MoveInstantly(new Vector2(-speedX * timeMult, -speedY * timeMult), MoveType.Relative, true);

                if ((upgrades & 0x1) == 0) {
                    DecreaseHealth(int.MaxValue);
                }
            }

            if (!fired) {
                fired = true;

                MoveInstantly(gunspotPos, MoveType.Absolute, true);
                renderer.Active = true;
            }

            base.OnFixedUpdate(timeMult);
        }

        protected override void OnRicochet()
        {
            speedY = speedY * -0.2f * (MathF.Rnd.Next() % 100 - 50);
            speedX = speedX * -0.2f + (MathF.Rnd.Next() % 100 - 50) * 0.02f;
        }
    }
}