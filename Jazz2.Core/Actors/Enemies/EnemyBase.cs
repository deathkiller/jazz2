using System.Collections.Generic;
using Duality;
using Duality.Drawing;
using Jazz2.Actors.Weapons;
using Jazz2.Game;
using Jazz2.Game.Events;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public abstract class EnemyBase : ActorBase
    {
        protected enum LastHitDirection
        {
            None,
            Left,
            Right,
            Up,
            Down
        }

        protected int scoreValue;
        protected bool canHurtPlayer = true;
        protected bool isAttacking;
        protected LastHitDirection lastHitDir;

        private float blinkingTimeout;

        public bool CanHurtPlayer
        {
            get
            {
                return (canHurtPlayer && frozenTimeLeft <= 0f);
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            HandleBlinking();
        }

        protected override void OnHealthChanged(ActorBase collider)
        {
            StartBlinking();
        }

        protected void SetHealthByDifficulty(int health)
        {
            switch (api.Difficulty) {
                case GameDifficulty.Easy: health = (int)MathF.Round(health * 0.6f); break;
                case GameDifficulty.Hard: health = (int)MathF.Round(health * 1.4f); break;
            }

            this.maxHealth = this.health = MathF.Max(health, 1);
        }

        protected bool CanMoveToPosition(float x, float y)
        {
            Vector3 pos = Transform.Pos;

            int sign = (isFacingLeft ? -1 : 1);

            EventMap events = api.EventMap;

            Hitbox h1 = currentHitbox + new Vector2(x, y - 10);
            Hitbox h2 = currentHitbox + new Vector2(x, y + 2);
            //Hitbox h3 = currentHitbox + new Vector2(x + sign * (currentHitbox.Right - currentHitbox.Left) / 2, y + 32);
            Hitbox h3 = currentHitbox + new Vector2(x + sign * (currentHitbox.Right - currentHitbox.Left) / 2, y + 12);

            ushort[] p = null;
            return ((api.IsPositionEmpty(ref h1, false, this) || api.IsPositionEmpty(ref h2, false, this))
                     && (events != null && events.GetEventByPosition(pos.X + x, pos.Y + y, ref p) != EventType.AreaStopEnemy)
                     && !api.IsPositionEmpty(ref h3, true, this));
        }

        protected void TryGenerateRandomDrop()
        {
            EventType drop = MathF.Rnd.OneOfWeighted(
                new KeyValuePair<EventType, float>(EventType.Empty, 10),
                new KeyValuePair<EventType, float>(EventType.Carrot, 2),
                new KeyValuePair<EventType, float>(EventType.FastFire, 2),
                new KeyValuePair<EventType, float>(EventType.Gem, 6)
            );

            if (drop != EventType.Empty) {
                ActorBase actor = api.EventSpawner.SpawnEvent(ActorInstantiationFlags.None, drop, Transform.Pos, new ushort[8]);
                api.AddActor(actor);
            }
        }

        protected void HandleBlinking()
        {
            if (blinkingTimeout > 0f) {
                blinkingTimeout -= Time.TimeMult;

                if (blinkingTimeout <= 0f) {
                    // Reset renderer
                    renderer.CustomMaterial = null;
                }
            }
        }

        protected void StartBlinking()
        {
            if (blinkingTimeout <= 0f) {
                BatchInfo blinkMaterial = renderer.SharedMaterial.Res.Info;
                blinkMaterial.Technique = ContentResolver.Current.RequestShader("Colorize");
                blinkMaterial.MainColor = new ColorRgba(1f, 0.5f);
                renderer.CustomMaterial = blinkMaterial;
            }

            blinkingTimeout = 6f;
        }

        public override void HandleCollision(ActorBase other)
        {
            base.HandleCollision(other);

            // ToDo: Use actor type specifying function instead when available
            if (!isInvulnerable) {
                AmmoBase ammo = other as AmmoBase;
                if (ammo != null) {
                    DecreaseHealth(ammo.Strength, ammo);
                    Vector3 ammoSpeed = ammo.Speed;
                    if (MathF.Abs(ammoSpeed.X) > float.Epsilon) {
                        lastHitDir = (ammoSpeed.X > 0 ? LastHitDirection.Right : LastHitDirection.Left);
                    } else {
                        lastHitDir = (ammoSpeed.Y > 0 ? LastHitDirection.Down : LastHitDirection.Up);
                    }
                }
            }
        }
    }
}