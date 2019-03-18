using Duality;
using Jazz2.Game;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;

namespace Jazz2.Actors.Weapons
{
    public class AmmoToaster : AmmoBase
    {
        public override WeaponType WeaponType => WeaponType.Toaster;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            collisionFlags &= ~CollisionFlags.ApplyGravitation;

            strength = 1;

            RequestMetadata("Weapon/Toaster");

            LightEmitter light = AddComponent<LightEmitter>();
            light.Intensity = 0.85f;
            light.Brightness = 0.4f;
            light.RadiusNear = 0f;
            light.RadiusFar = 30f;
        }

        public void OnFire(Player owner, Vector3 speed, float angle, bool isFacingLeft, byte upgrades)
        {
            base.owner = owner;
            base.IsFacingLeft = isFacingLeft;
            base.upgrades = upgrades;

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
            speedY += MathF.Abs(speed.Y) * speedY;
            speedY += ax * MathF.Rnd.NextFloat(-0.5f, 0.5f);

            AnimState state = AnimState.Idle;
            if ((upgrades & 0x1) != 0) {
                timeLeft = 80;
                state |= (AnimState)1;
            } else {
                timeLeft = 60;
            }

            SetAnimation(state);
        }

        protected override void OnUpdate()
        {
            OnUpdateHitbox();

            Vector3 pos = Transform.Pos;

            if (pos.Y >= api.WaterLevel) {
                DecreaseHealth(int.MaxValue);
                return;
            }

            TileMap tiles = api.TileMap;
            if (tiles == null || tiles.IsTileEmpty(ref currentHitbox, false)) {
                MoveInstantly(new Vector2(speedX, speedY), MoveType.RelativeTime, true);
                CheckCollisions(Time.TimeMult);
            } else {
                MoveInstantly(new Vector2(speedX, speedY), MoveType.RelativeTime, true);
                CheckCollisions(Time.TimeMult);
                MoveInstantly(new Vector2(-speedX, -speedY), MoveType.RelativeTime, true);

                if ((upgrades & 0x1) == 0) {
                    DecreaseHealth(int.MaxValue);
                }
            }

            base.OnUpdate();
        }

        protected override void OnRicochet()
        {
            speedY = speedY * -0.2f * (MathF.Rnd.Next() % 100 - 50);
            speedX = speedX * -0.2f + (MathF.Rnd.Next() % 100 - 50) * 0.02f;
        }
    }
}