using System.Collections.Generic;
using Duality;
using Duality.Drawing;
using Duality.Resources;
using Jazz2.Actors.Weapons;
using Jazz2.Game;
using Jazz2.Game.Events;
using Jazz2.Game.Structs;
using static Jazz2.Game.Tiles.TileMap;

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
            return ((api.IsPositionEmpty(this, ref h1, true) || api.IsPositionEmpty(this, ref h2, true))
                     && (events != null && events.GetEventByPosition(pos.X + x, pos.Y + y, ref p) != EventType.AreaStopEnemy)
                     && !api.IsPositionEmpty(this, ref h3, true));
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

        protected override void OnAnimationStarted()
        {
            if (blinkingTimeout < 1f) {
                // Reset renderer
                renderer.CustomMaterial = null;
            } else {
                // Refresh temporary material
                BatchInfo blinkMaterial = renderer.SharedMaterial.Res.Info;
                blinkMaterial.Technique = ContentResolver.Current.RequestShader("Colorize");
                blinkMaterial.MainColor = new ColorRgba(1f, 0.5f);
                renderer.CustomMaterial = blinkMaterial;
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
                // Create temporary material
                BatchInfo blinkMaterial = renderer.SharedMaterial.Res.Info;
                blinkMaterial.Technique = ContentResolver.Current.RequestShader("Colorize");
                blinkMaterial.MainColor = new ColorRgba(1f, 0.5f);
                renderer.CustomMaterial = blinkMaterial;
            }

            blinkingTimeout = 6f;
        }

        protected void CreateDeathDebris(ActorBase collider)
        {
            if (collider is AmmoToaster) {
                const int debrisSizeX = 5;
                const int debrisSizeY = 3;

                GraphicResource res = currentTransitionState != AnimState.Idle ? currentTransition : currentAnimation;
                Material material = res.Material.Res;
                Texture texture = material.MainTexture.Res;

                Vector3 pos = Transform.Pos;

                float x = pos.X - res.Base.Hotspot.X;
                float y = pos.Y - res.Base.Hotspot.Y;

                int currentFrame = renderer.CurrentFrame;

                for (int fx = 0; fx < res.Base.FrameDimensions.X; fx += debrisSizeX + 1) {
                    for (int fy = 0; fy < res.Base.FrameDimensions.Y; fy += debrisSizeY + 1) {
                        float currentSizeX = debrisSizeX * MathF.Rnd.NextFloat(0.8f, 1.1f);
                        float currentSizeY = debrisSizeY * MathF.Rnd.NextFloat(0.8f, 1.1f);
                        api.TileMap.CreateDebris(new DestructibleDebris {
                            Pos = new Vector3(x + (isFacingLeft ? res.Base.FrameDimensions.X - fx : fx), y + fy, pos.Z),
                            Size = new Vector2(currentSizeX, currentSizeY),
                            Speed = new Vector2(((fx - res.Base.FrameDimensions.X / 2) + MathF.Rnd.NextFloat(-2f, 2f)) * (isFacingLeft ? -1f : 1f) * MathF.Rnd.NextFloat(0.5f, 2f) / res.Base.FrameDimensions.X,
                                 MathF.Rnd.NextFloat(0f, 0.2f)),
                            Acceleration = new Vector2(0f, 0.06f),

                            Scale = 1f,
                            Alpha = 1f,
                            AlphaSpeed = -0.002f,

                            Time = 320f,

                            Material = material,
                            MaterialOffset = new Rect(
                                 (((float)(currentFrame % res.Base.FrameConfiguration.X) / res.Base.FrameConfiguration.X) + ((float)fx / texture.ContentWidth)) * texture.UVRatio.X,
                                 (((float)(currentFrame / res.Base.FrameConfiguration.X) / res.Base.FrameConfiguration.Y) + ((float)fy / texture.ContentHeight)) * texture.UVRatio.Y,
                                 (currentSizeX * texture.UVRatio.X / texture.ContentWidth),
                                 (currentSizeY * texture.UVRatio.Y / texture.ContentHeight)
                             ),

                            CollisionAction = DebrisCollisionAction.Bounce
                        });
                    }
                }
            } else {
                CreateParticleDebris();
            }
        }

        public override void HandleCollision(ActorBase other)
        {
            base.HandleCollision(other);

            // ToDo: Use actor type specifying function instead when available
            if (!isInvulnerable) {
                switch (other) {
                    case AmmoBase collision: {
                        DecreaseHealth(collision.Strength, collision);
                        Vector3 ammoSpeed = collision.Speed;
                        if (MathF.Abs(ammoSpeed.X) > float.Epsilon) {
                            lastHitDir = (ammoSpeed.X > 0 ? LastHitDirection.Right : LastHitDirection.Left);
                        } else {
                            lastHitDir = (ammoSpeed.Y > 0 ? LastHitDirection.Down : LastHitDirection.Up);
                        }
                        break;
                    }
                    case AmmoTNT collision: {
                        DecreaseHealth(5, collision);
                        break;
                    }
                }
            }
        }
    }
}