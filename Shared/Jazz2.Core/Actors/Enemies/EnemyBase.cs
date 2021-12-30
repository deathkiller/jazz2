﻿using System.Collections.Generic;
using Duality;
using Duality.Drawing;
using Duality.Resources;
using Jazz2.Actors.Weapons;
using Jazz2.Game;
using Jazz2.Game.Collisions;
using Jazz2.Game.Events;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;
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

        protected uint scoreValue;
        protected bool canHurtPlayer = true;
        protected bool canCollideWithAmmo = true;
        protected LastHitDirection lastHitDir;

        private float blinkingTimeout;

        public bool CanHurtPlayer => (canHurtPlayer && frozenTimeLeft <= 0f);

        public bool CanCollideWithAmmo => canCollideWithAmmo;

        public override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            HandleBlinking(timeMult);
        }

        protected override void OnHealthChanged(ActorBase collider)
        {
            StartBlinking();
        }

        protected void SetHealthByDifficulty(int health)
        {
            switch (levelHandler.Difficulty) {
                case GameDifficulty.Easy: health = (int)MathF.Round(health * 0.6f); break;
                case GameDifficulty.Hard: health = (int)MathF.Round(health * 1.4f); break;
            }

            this.health = MathF.Max(health, 1);
            this.maxHealth = this.health;
        }

        protected bool CanMoveToPosition(float x, float y)
        {
            Vector3 pos = Transform.Pos;

            int direction = (IsFacingLeft ? -1 : 1);

            EventMap events = levelHandler.EventMap;

            AABB aabbA = AABBInner + new Vector2(x, y - 3);
            AABB aabbB = AABBInner + new Vector2(x, y + 3);
            if (!levelHandler.IsPositionEmpty(this, ref aabbA, true) && !levelHandler.IsPositionEmpty(this, ref aabbB, true)) {
                return false;
            }

            AABB aabbDir = AABBInner + new Vector2(x + direction * (AABBInner.UpperBound.X - AABBInner.LowerBound.X) * 0.5f, y + 12);

            ushort[] p = null;
            return ((events == null || events.GetEventByPosition(pos.X + x, pos.Y + y, ref p) != EventType.AreaStopEnemy) && !levelHandler.IsPositionEmpty(this, ref aabbDir, true));
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
                ActorBase actor = levelHandler.EventSpawner.SpawnEvent(drop, new ushort[8], ActorInstantiationFlags.None, Transform.Pos);
                levelHandler.AddActor(actor);
            }
        }

#if !SERVER
        protected override void OnAnimationStarted()
        {
            if (blinkingTimeout < 1f) {
                // Reset renderer
                renderer.CustomMaterial = null;
            } else {
                // Refresh temporary material
                BatchInfo blinkMaterial = new BatchInfo(renderer.SharedMaterial.Res.Info);
                blinkMaterial.Technique = ContentResolver.Current.RequestShader("Colorize");
                blinkMaterial.MainColor = new ColorRgba(1f, 0.5f);
                renderer.CustomMaterial = blinkMaterial;
            }
        }
#endif

        protected void HandleBlinking(float timeMult)
        {
            if (blinkingTimeout > 0f) {
                blinkingTimeout -= timeMult;

#if !SERVER
                if (blinkingTimeout <= 0f) {
                    // Reset renderer
                    renderer.CustomMaterial = null;
                }
#endif
            }
        }

        protected void StartBlinking()
        {
            if (blinkingTimeout <= 0f) {
#if !SERVER
                // Create temporary material
                BatchInfo blinkMaterial = new BatchInfo(renderer.SharedMaterial.Res.Info);
                blinkMaterial.Technique = ContentResolver.Current.RequestShader("Colorize");
                blinkMaterial.MainColor = new ColorRgba(1f, 0.5f);
                renderer.CustomMaterial = blinkMaterial;
#endif
            }

            blinkingTimeout = 6f;
        }

        protected void CreateDeathDebris(ActorBase collider)
        {
            TileMap tilemap = levelHandler.TileMap;
            if (tilemap == null) {
                return;
            }

            Vector3 pos = Transform.Pos;

            if (collider is AmmoToaster) {
                const int debrisSizeX = 5;
                const int debrisSizeY = 3;

                GraphicResource res = currentTransitionState != AnimState.Idle ? currentTransition : currentAnimation;
                Material material = res.Material.Res;
                Texture texture = material.MainTexture.Res;

                float x = pos.X - res.Base.Hotspot.X;
                float y = pos.Y - res.Base.Hotspot.Y;

                int currentFrame = renderer.CurrentFrame;

                for (int fx = 0; fx < res.Base.FrameDimensions.X; fx += debrisSizeX + 1) {
                    for (int fy = 0; fy < res.Base.FrameDimensions.Y; fy += debrisSizeY + 1) {
                        float currentSizeX = debrisSizeX * MathF.Rnd.NextFloat(0.8f, 1.1f);
                        float currentSizeY = debrisSizeY * MathF.Rnd.NextFloat(0.8f, 1.1f);
                        levelHandler.TileMap.CreateDebris(new DestructibleDebris {
                            Pos = new Vector3(x + (IsFacingLeft ? res.Base.FrameDimensions.X - fx : fx), y + fy, pos.Z),
                            Size = new Vector2(currentSizeX, currentSizeY),
                            Speed = new Vector2(((fx - res.Base.FrameDimensions.X / 2) + MathF.Rnd.NextFloat(-2f, 2f)) * (IsFacingLeft ? -1f : 1f) * MathF.Rnd.NextFloat(0.5f, 2f) / res.Base.FrameDimensions.X,
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
            } else if (pos.Y > levelHandler.WaterLevel) {
                const int DebrisSize = 3;

                GraphicResource res = currentTransitionState != AnimState.Idle ? currentTransition : currentAnimation;
                Material material = res.Material.Res;
                Texture texture = material.MainTexture.Res;

                float x = pos.X - res.Base.Hotspot.X;
                float y = pos.Y - res.Base.Hotspot.Y;

                for (int fx = 0; fx < res.Base.FrameDimensions.X; fx += DebrisSize + 1) {
                    for (int fy = 0; fy < res.Base.FrameDimensions.Y; fy += DebrisSize + 1) {
                        float currentSize = DebrisSize * MathF.Rnd.NextFloat(0.2f, 1.1f);
                        levelHandler.TileMap.CreateDebris(new DestructibleDebris {
                            Pos = new Vector3(x + (IsFacingLeft ? res.Base.FrameDimensions.X - fx : fx), y + fy, pos.Z),
                            Size = new Vector2(currentSize /** (isFacingLeft ? -1f : 1f)*/, currentSize),
                            Speed = new Vector2(((fx - res.Base.FrameDimensions.X / 2) + MathF.Rnd.NextFloat(-2f, 2f)) * (IsFacingLeft ? -1f : 1f) * MathF.Rnd.NextFloat(1f, 3f) / res.Base.FrameDimensions.X,
                                 ((fy - res.Base.FrameDimensions.Y / 2) + MathF.Rnd.NextFloat(-2f, 2f)) * (IsFacingLeft ? -1f : 1f) * MathF.Rnd.NextFloat(1f, 3f) / res.Base.FrameDimensions.Y),
                            Acceleration = new Vector2(0f, 0f),

                            Scale = 1f,
                            Alpha = 1f,
                            AlphaSpeed = -0.004f,

                            Time = 340f,

                            Material = material,
                            MaterialOffset = new Rect(
                                 (((float)(renderer.CurrentFrame % res.Base.FrameConfiguration.X) / res.Base.FrameConfiguration.X) + ((float)fx / texture.ContentWidth)) * texture.UVRatio.X,
                                 (((float)(renderer.CurrentFrame / res.Base.FrameConfiguration.X) / res.Base.FrameConfiguration.Y) + ((float)fy / texture.ContentHeight)) * texture.UVRatio.Y,
                                 (currentSize * texture.UVRatio.X / texture.ContentWidth),
                                 (currentSize * texture.UVRatio.Y / texture.ContentHeight)
                             ),

                            CollisionAction = DebrisCollisionAction.Disappear
                        });
                    }
                }
            } else {
                Vector2 force;
                switch (lastHitDir) {
                    case LastHitDirection.Left: force = new Vector2(-1.4f, 0f); break;
                    case LastHitDirection.Right: force = new Vector2(1.4f, 0f); break;
                    case LastHitDirection.Up: force = new Vector2(0f, -1.4f); break;
                    case LastHitDirection.Down: force = new Vector2(0f, 1.4f); break;

                    default: force = Vector2.Zero; break;
                }

                tilemap.CreateParticleDebris(currentTransitionState != AnimState.Idle ? currentTransition : currentAnimation,
                    Transform.Pos, force, renderer.CurrentFrame, IsFacingLeft);
            }
        }

        public override void OnHandleCollision(ActorBase other)
        {
            base.OnHandleCollision(other);

            if (!isInvulnerable) {
                switch (other) {
                    case AmmoBase collision: {
                        Vector3 ammoSpeed = collision.Speed;
                        if (MathF.Abs(ammoSpeed.X) > 0.2f) {
                            lastHitDir = (ammoSpeed.X > 0 ? LastHitDirection.Right : LastHitDirection.Left);
                        } else {
                            lastHitDir = (ammoSpeed.Y > 0 ? LastHitDirection.Down : LastHitDirection.Up);
                        }
                        DecreaseHealth(collision.Strength, collision);
                        break;
                    }
                    case AmmoTNT collision: {
                        DecreaseHealth(5, collision);
                        break;
                    }
                }
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            switch (collider) {
                case Player player: {
                    player.AddScore(scoreValue);
                    break;
                }
                case AmmoBase ammo: {
                    ammo.Owner?.AddScore(scoreValue);
                    break;
                }
                case AmmoTNT ammo: {
                    ammo.Owner?.AddScore(scoreValue);
                    break;
                }
            }

            return base.OnPerish(collider);
        }
    }
}