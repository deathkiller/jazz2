using System.Threading.Tasks;
using Duality;
using Jazz2.Actors.Enemies;
using Jazz2.Game;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Weapons
{
    // ToDo: Adjust according to https://www.jazz2online.com/wiki/seeker

    public partial class AmmoSeeker : AmmoBase
    {
        private Vector2 gunspotPos;
        private bool fired;

        private float defaultRecomputeTime;

        private float followRecomputeTime;

        public override WeaponType WeaponType => WeaponType.Seeker;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await base.OnActivatedAsync(details);

            base.upgrades = (byte)details.Params[0];

            //strength = 2;
            collisionFlags &= ~CollisionFlags.ApplyGravitation;

            await RequestMetadataAsync("Weapon/Seeker");

            AnimState state = AnimState.Idle;
            if ((upgrades & 0x1) != 0) {
                timeLeft = 188;
                defaultRecomputeTime = 6f;
                strength = 3;
                state |= (AnimState)1;
            } else {
                timeLeft = 144;
                defaultRecomputeTime = 10f;
                strength = 2;
            }

            SetAnimation(state);
            PlaySound(Transform.Pos, "Fire");

            renderer.Active = false;

            LightEmitter light = AddComponent<LightEmitter>();
            light.Intensity = 0.8f;
            light.RadiusNear = 3f;
            light.RadiusFar = 8f;
        }

        public void OnFire(Player owner, Vector3 gunspotPos, Vector3 speed, float angle, bool isFacingLeft)
        {
            base.owner = owner;
            //base.isFacingLeft = isFacingLeft;

            this.gunspotPos = gunspotPos.Xy;

            float angleRel = angle * (isFacingLeft ? -1 : 1);

            const float baseSpeed = 1.85f;
            if (isFacingLeft) {
                speedX = MathF.Min(0, speed.X * 0.06f) - MathF.Cos(angleRel) * baseSpeed;
            } else {
                speedX = MathF.Max(0, speed.X * 0.06f) + MathF.Cos(angleRel) * baseSpeed;
            }
            speedY = MathF.Sin(angleRel) * baseSpeed;

            Transform.Angle = angleRel;
        }

        public override void OnFixedUpdate(float timeMult)
        {
            // Seeker is slow, so it's not neccessary to do two-pass checking
            TryMovement(timeMult);
            OnUpdateHitbox();
            CheckCollisions(timeMult);

            FollowNeareastEnemy(timeMult);

            if (!fired) {
                fired = true;

                MoveInstantly(gunspotPos, MoveType.Absolute, true);
                renderer.Active = true;
            }

            base.OnFixedUpdate(timeMult);
        }

        protected override bool OnPerish(ActorBase collider)
        {
            Vector3 pos = Transform.Pos;

            levelHandler.FindCollisionActorsByRadius(pos.X, pos.Y, 36, actor => {
                Player player = actor as Player;
                if (player != null) {
                    bool pushLeft = (pos.X > player.Transform.Pos.X);
                    player.AddExternalForce(pushLeft ? -8f : 8f, 0f);
                }
                return true;
            });

            Explosion.Create(levelHandler, pos + Speed, Explosion.Large);

            return base.OnPerish(collider);
        }

        protected override void OnHitWall()
        {
            DecreaseHealth(int.MaxValue);
        }

        protected override void OnRicochet()
        {
        }

        private void FollowNeareastEnemy(float timeMult)
        {
            if (followRecomputeTime > 0f) {
                followRecomputeTime -= timeMult;
                return;
            }

            bool found = false;
            Vector3 pos = Transform.Pos;
            Vector3 targetPos = new Vector3(float.MaxValue, float.MaxValue, pos.Z);

            foreach (GameObject obj in levelHandler.ActiveObjects) {
                EnemyBase enemy = obj as EnemyBase;
                if (enemy != null && !enemy.IsInvulnerable && enemy.CanCollideWithAmmo) {
                    Vector3 newPos = enemy.Transform.Pos;
                    if ((pos - newPos).Length < (pos - targetPos).Length) {
                        targetPos = newPos;
                        found = true;
                    }
                }
            }

            if (found) {
                Vector3 diff = (targetPos - pos);
                if (diff.Length < 260f) {
                    Vector3 speed = (new Vector3(speedX, speedY, 0f) + diff.Normalized * 2f).Normalized;
                    speedX = speed.X * 2.6f;
                    speedY = speed.Y * 2.6f; 
                }
            }

            Transform.Angle = MathF.Atan2(speedY, speedX);

            followRecomputeTime = defaultRecomputeTime;
        }
    }
}