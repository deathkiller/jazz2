using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Duality;
using Duality.Audio;
using Duality.Components;
using Duality.Components.Renderers;
using Duality.Drawing;
using Jazz2.Actors.Lighting;
using Jazz2.Actors.Weapons;
using Jazz2.Game;
using Jazz2.Game.Components;
using Jazz2.Game.Events;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;
using static Duality.Component;

namespace Jazz2.Actors
{
    [Flags]
    public enum ActorInstantiationFlags : byte
    {
        None = 0,

        IsCreatedFromEventMap = 1 << 0,
        IsFromGenerator = 1 << 1,

        Illuminated = 1 << 2,
    }

    public struct ActorInstantiationDetails
    {
        public ActorApi Api;
        public Vector3 Pos;
        public ActorInstantiationFlags Flags;
        public ushort[] Params;
    }

    [Flags]
    public enum CollisionFlags : byte
    {
        None = 0,

        CollideWithTileset = 1 << 0,
        CollideWithOtherActors = 1 << 1,
        CollideWithSolidObjects = 1 << 2,

        ApplyGravitation = 1 << 5,
        IsSolidObject = 1 << 6,
        SkipPerPixelCollisions = 1 << 7,
    }

    public enum MoveType
    {
        Absolute,
        Relative,
        RelativeTime
    }

    public abstract class ActorBase : GameObject
    {
        private const float CollisionCheckStep = 0.5f;

        protected ActorApi api;

        protected int maxHealth = 1;
        protected int health = 1;

        protected float speedX, speedY;
        protected float externalForceX, externalForceY;
        protected float internalForceY;
        protected float elasticity;
        protected float friction;
        protected bool canJump;
        protected bool canBeFrozen = true;

        protected bool isFacingLeft;
        protected bool isInvulnerable;
        protected CollisionFlags collisionFlags = CollisionFlags.CollideWithTileset | CollisionFlags.CollideWithOtherActors | CollisionFlags.ApplyGravitation;

        protected float frozenTimeLeft;

        protected SuspendType suspendType;
        protected Point2 originTile;
        protected ActorInstantiationFlags flags;

        protected Hitbox currentHitbox;

        protected ActorRenderer renderer;
        private Point2 boundingBox;

        protected Dictionary<string, GraphicResource> availableAnimations;
        protected GraphicResource currentAnimation;
        protected GraphicResource currentTransition;
        protected Dictionary<string, SoundResource> availableSounds;

        protected AnimState currentAnimationState = AnimState.StateUninitialized;
        protected AnimState currentTransitionState;
        protected bool currentTransitionCancellable;
        private Action currentTransitionCallback;

        public Hitbox Hitbox => currentHitbox;
        public CollisionFlags CollisionFlags => collisionFlags;
        public bool IsInvulnerable => isInvulnerable;
        public int Health => health;
        public int MaxHealth => maxHealth;

        public Vector3 Speed => new Vector3(speedX, speedY, 0f);
        public Vector3 ExternalForce => new Vector3(externalForceX, externalForceY, 0f);
        public Vector3 InternalForce => new Vector3(0, internalForceY, 0f);

        public virtual void OnAttach(ActorInstantiationDetails details)
        {
            this.api = details.Api;
            this.flags = details.Flags;

            friction = 1.5f;

            originTile = new Point2((int)(details.Pos.X / 32), (int)(details.Pos.Y / 32));

            Transform transform = AddComponent<Transform>();
            transform.Pos = details.Pos;

            AddComponent(new LocalController(this));

            OnUpdateHitbox();
        }

        protected virtual void OnUpdateHitbox()
        {
            UpdateHitbox(boundingBox.X, boundingBox.Y);
        }

        protected void UpdateHitbox(int w, int h)
        {
            if (currentAnimation == null) {
                return;
            }

            Vector3 pos = Transform.Pos;

            if (currentAnimation.Base.HasColdspot) {
                currentHitbox = new Hitbox(
                    pos.X - currentAnimation.Base.Hotspot.X + currentAnimation.Base.Coldspot.X - (w / 2),
                    pos.Y - currentAnimation.Base.Hotspot.Y + currentAnimation.Base.Coldspot.Y - h,
                    pos.X - currentAnimation.Base.Hotspot.X + currentAnimation.Base.Coldspot.X + (w / 2),
                    pos.Y - currentAnimation.Base.Hotspot.Y + currentAnimation.Base.Coldspot.Y
                );
            } else {
                // Collision base set to the bottom of the sprite.
                // This is probably still not the correct way to do it, but at least it works for now.
                currentHitbox = new Hitbox(
                    pos.X - (w / 2),
                    pos.Y - currentAnimation.Base.Hotspot.Y + currentAnimation.Base.FrameDimensions.Y - h,
                    pos.X + (w / 2),
                    pos.Y - currentAnimation.Base.Hotspot.Y + currentAnimation.Base.FrameDimensions.Y
                );
            }
        }

        public void DecreaseHealth(int amount = 1, ActorBase collider = null)
        {
            if (amount > health) {
                health = 0;
            } else {
                health -= amount;
            }

            if (health <= 0) {
                OnPerish(collider);
            } else {
                OnHealthChanged(collider);
            }
        }

        protected virtual void OnHealthChanged(ActorBase collider)
        {
        }

        protected virtual bool OnPerish(ActorBase collider)
        {
            EventMap events = api.EventMap;
            if (events != null && (flags & ActorInstantiationFlags.IsCreatedFromEventMap) != 0) {
                events.Deactivate(originTile.X, originTile.Y);
                events.StoreTileEvent(originTile.X, originTile.Y, EventType.Empty);
            }

            api.RemoveActor(this);

            return true;
        }

        protected virtual void OnHitFloorHook()
        {
            // Called from inside the position update code when the object hits floor
            // and was falling earlier. Objects should override this if they need to
            // (e.g. the Player class playing a sound).
        }

        protected virtual void OnHitCeilingHook()
        {
            // Called from inside the position update code when the object hits ceiling.
            // Objects should override this if they need to.
        }

        protected virtual void OnHitWallHook()
        {
            // Called from inside the position update code when the object hits a wall.
            // Objects should override this if they need to.
        }

        protected void TryStandardMovement(float timeMult)
        {
            float gravity = ((collisionFlags & CollisionFlags.ApplyGravitation) != 0 ? api.Gravity : 0);

            speedX = MathF.Clamp(speedX, -16f, 16f);
            speedY = MathF.Clamp(speedY - (internalForceY + externalForceY) * timeMult, -16f, 16f);

            float effectiveSpeedX, effectiveSpeedY;
            if (frozenTimeLeft > 0f) {
                effectiveSpeedX = MathF.Clamp(externalForceX * timeMult, -16f, 16f);
                effectiveSpeedY = MathF.Clamp((/*speedY*/(gravity * 2f) + internalForceY) * timeMult, -16f, 16f);
            } else {
                effectiveSpeedX = speedX + externalForceX * timeMult;
                effectiveSpeedY = speedY;
            }
            effectiveSpeedX *= timeMult;
            effectiveSpeedY *= timeMult;

            bool success = false;

            if (canJump) {
                // All ground-bound movement is handled here. In the basic case, the actor
                // moves horizontally, but it can also logically move up or down if it is
                // moving across a slope. In here, angles between about 45 degrees down
                // to 45 degrees up are attempted with some intervals to attempt to keep
                // the actor attached to the slope in question.

                // Always try values a bit over the 45 degree incline; subpixel coordinates
                // may mean the actor actually needs to move a pixel up or down even though
                // the speed wouldn't warrant that large of a change.
                // Not doing this will cause hiccups with uphill slopes in particular.
                // Beach tileset also has some spots where two properly set up adjacent
                // tiles have a 2px jump, so adapt to that.
                float maxYDiff = MathF.Max(3.0f, MathF.Abs(effectiveSpeedX) + 2.5f);
                for (float yDiff = maxYDiff + effectiveSpeedY; yDiff >= -maxYDiff + effectiveSpeedY; yDiff -= CollisionCheckStep) {
                    if (MoveInstantly(new Vector2(effectiveSpeedX, yDiff), MoveType.Relative)) {
                        success = true;
                        break;
                    }
                }

                // Also try to move horizontally as far as possible.
                float xDiff = MathF.Abs(effectiveSpeedX);
                float maxXDiff = -xDiff;
                if (!success) {
                    int sign = (effectiveSpeedX > 0f ? 1 : -1);
                    for (; xDiff >= maxXDiff; xDiff -= CollisionCheckStep) {
                        if (MoveInstantly(new Vector2(xDiff * sign, 0f), MoveType.Relative)) {
                            break;
                        }
                    }

                    // If no angle worked in the previous step, the actor is facing a wall.
                    if (xDiff > CollisionCheckStep || (xDiff > 0f && elasticity > 0f)) {
                        speedX = -(elasticity * speedX);
                    }
                    OnHitWallHook();
                }

                // Run all floor-related hooks, such as the player's check for hurting positions.
                OnHitFloorHook();
            } else {
                // Airborne movement is handled here.
                // First, attempt to move directly based on the current speed values.
                if (MoveInstantly(new Vector2(effectiveSpeedX, effectiveSpeedY), MoveType.Relative)) {
                    if (MathF.Abs(effectiveSpeedY) < float.Epsilon) {
                        canJump = true;
                    }
                } else if (!success) {
                    // There is an obstacle so we need to make compromises.

                    // First, attempt to move horizontally as much as possible.
                    float maxDiff = MathF.Abs(effectiveSpeedX);
                    int sign = (effectiveSpeedX > 0f ? 1 : -1);
                    float xDiff = maxDiff;
                    for (; xDiff > float.Epsilon; xDiff -= CollisionCheckStep) {
                        if (MoveInstantly(new Vector2(xDiff * sign, 0f), MoveType.Relative)) {
                            break;
                        }
                    }

                    // Then, try the same vertically.
                    maxDiff = MathF.Abs(effectiveSpeedY);
                    sign = (effectiveSpeedY > 0f ? 1 : -1);
                    float yDiff = maxDiff;
                    for (; yDiff > float.Epsilon; yDiff -= CollisionCheckStep) {
                        float yDiffSigned = (yDiff * sign);
                        if (MoveInstantly(new Vector2(0f, yDiffSigned), MoveType.Relative) ||
                            // Add horizontal tolerance
                            MoveInstantly(new Vector2(yDiff *  0.2f, yDiffSigned), MoveType.Relative) ||
                            MoveInstantly(new Vector2(yDiff * -0.2f, yDiffSigned), MoveType.Relative)) {
                            break;
                        }
                    }

                    // Place us to the ground only if no horizontal movement was
                    // involved (this prevents speeds resetting if the actor
                    // collides with a wall from the side while in the air)
                    if (yDiff < Math.Abs(effectiveSpeedY)) {
                        if (effectiveSpeedY > 0f) {
                            speedY = -(elasticity * effectiveSpeedY / timeMult);
                            
                            OnHitFloorHook();

                            if (speedY > -CollisionCheckStep) {
                                speedY = 0f;
                                canJump = true;
                            }
                        } else {
                            speedY = 0f;
                            OnHitCeilingHook();
                        }
                    }

                    // If the actor didn't move all the way horizontally,
                    // it hit a wall (or was already touching it)
                    if (xDiff < MathF.Abs(effectiveSpeedX)) {
                        if (xDiff > CollisionCheckStep || (xDiff > 0f && elasticity > 0f)) {
                            speedX = -(elasticity * speedX);
                        }
                        OnHitWallHook();
                    }
                }
            }

            // Set the actor as airborne if there seems to be enough space below it
            Hitbox hitbox = (currentHitbox + new Vector2(0f, CollisionCheckStep));
            if (api.IsPositionEmpty(this, ref hitbox, effectiveSpeedY >= 0)) {
                speedY += gravity * timeMult;
                canJump = false;
            }

            // Reduce all forces if they are present
            if (MathF.Abs(externalForceX) > float.Epsilon) {
                if (externalForceX > 0f) {
                    externalForceX = MathF.Max(externalForceX - friction * timeMult, 0f);
                } else {
                    externalForceX = MathF.Min(externalForceX + friction * timeMult, 0f);
                }
            }
            externalForceY = MathF.Max(externalForceY - gravity * 0.33f * timeMult, 0f);
            internalForceY = MathF.Max(internalForceY - gravity * 0.33f * timeMult, 0f);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected void RefreshFlipMode()
        {
            if (renderer != null) {
                renderer.Flip = (isFacingLeft ? SpriteRenderer.FlipMode.Horizontal : SpriteRenderer.FlipMode.None);
            }
        }

        public virtual bool OnTileDeactivate(int tx, int ty, int tileDistance)
        {
            if ((flags & (ActorInstantiationFlags.IsCreatedFromEventMap | ActorInstantiationFlags.IsFromGenerator)) != 0 && ((MathF.Abs(tx - originTile.X) > tileDistance) || (MathF.Abs(ty - originTile.Y) > tileDistance))) {
                EventMap events = api.EventMap;
                if (events != null) {
                    if ((flags & ActorInstantiationFlags.IsFromGenerator) != 0) {
                        events.ResetGenerator(originTile.X, originTile.Y);
                    }

                    events.Deactivate(originTile.X, originTile.Y);
                }

                api.RemoveActor(this);
                return true;
            }

            return false;
        }

        public virtual void HandleCollision(ActorBase other)
        {
            // Objects should override this if they need to.

            if (canBeFrozen) {
                HandleAmmoFrozenStateChange(other);
            }
        }

        public bool MoveInstantly(Vector2 pos, MoveType type, bool force = false)
        {
            Vector2 newPos;
            switch (type) {
                default:
                case MoveType.Absolute: newPos = pos; break;
                case MoveType.Relative: newPos = new Vector2(pos.X + Transform.Pos.X, pos.Y + Transform.Pos.Y); break;
                case MoveType.RelativeTime:
                    float mult = Time.TimeMult;
                    newPos = new Vector2(pos.X * mult + Transform.Pos.X, pos.Y * mult + Transform.Pos.Y);
                    break;
            }

            Hitbox translatedHitbox = currentHitbox + newPos - new Vector2(Transform.Pos.X, Transform.Pos.Y);

            // ToDo: Fix moving on roofs through windowsill in colon2
            bool free = force || api.IsPositionEmpty(this, ref translatedHitbox, speedY >= 0);
            if (free) {
                currentHitbox = translatedHitbox;
                Transform.Pos = new Vector3(newPos.X, newPos.Y, Transform.Pos.Z);
            }
            return free;
        }

        public void AddExternalForce(float x, float y)
        {
            externalForceX += x;
            externalForceY += y;
        }

        public void HandleAmmoFrozenStateChange(ActorBase ammo)
        {
            switch (ammo) {
                case AmmoFreezer freezer:
                    if (freezer.Owner != this) {
                        frozenTimeLeft = freezer.FrozenDuration;

                        if (renderer != null) {
                            renderer.AnimPaused = true;
                        }
                    }
                    break;

                case AmmoToaster toaster:
                    frozenTimeLeft = 0f;
                    break;
            }
        }

        protected virtual void OnUpdate()
        {
            float timeMult = Time.TimeMult;

            TryStandardMovement(timeMult);
            OnUpdateHitbox();

            RefreshFlipMode();

            if (renderer != null && renderer.AnimPaused) {
                if (frozenTimeLeft <= 0f) {
                    renderer.AnimPaused = false;
                } else {
                    frozenTimeLeft -= timeMult;
                }
            }
        }

        protected virtual void OnDeactivated(ShutdownContext context)
        {

        }

        public bool IsCollidingWith(ActorBase other)
        {
            bool perPixel1 = (collisionFlags & CollisionFlags.SkipPerPixelCollisions) == 0;
            bool perPixel2 = (other.collisionFlags & CollisionFlags.SkipPerPixelCollisions) == 0;

            // Limitation - both have to support per-pixel collisions
            if (perPixel1 && perPixel2 && (Transform.Angle != 0f || other.Transform.Angle != 0f)) {
                return IsCollidingWithAngled(other);
            }

            GraphicResource res1 = (currentTransitionState != AnimState.Idle ? currentTransition : currentAnimation);
            GraphicResource res2 = (other.currentTransitionState != AnimState.Idle ? other.currentTransition : other.currentAnimation);

            PixelData p1 = res1?.Material.Res?.MainTexture.Res?.BasePixmap.Res.PixelData?[0];
            PixelData p2 = res2?.Material.Res?.MainTexture.Res?.BasePixmap.Res.PixelData?[0];
            if (p1 == null || p2 == null) {
                return false;
            }

            Vector3 pos1 = Transform.Pos;
            Vector3 pos2 = other.Transform.Pos;

            Point2 hotspot1 = res1.Base.Hotspot;
            Point2 hotspot2 = res2.Base.Hotspot;

            Point2 size1 = res1.Base.FrameDimensions;
            Point2 size2 = res2.Base.FrameDimensions;

            Rect box1, box2;
            if (isFacingLeft) {
                box1 = new Rect(pos1.X + hotspot1.X - size1.X, pos1.Y - hotspot1.Y, size1.X, size1.Y);
            } else {
                box1 = new Rect(pos1.X - hotspot1.X, pos1.Y - hotspot1.Y, size1.X, size1.Y);
            }
            if (other.isFacingLeft) {
                box2 = new Rect(pos2.X + hotspot2.X - size2.X, pos2.Y - hotspot2.Y, size2.X, size2.Y);
            } else {
                box2 = new Rect(pos2.X - hotspot2.X, pos2.Y - hotspot2.Y, size2.X, size2.Y);
            }

            // Bounding Box intersection
            Rect inter = box1.Intersection(box2);
            if (inter.W <= 0 || inter.H <= 0) {
                return false;
            }

            if (!perPixel1 || !perPixel2) {
                if (perPixel1 == perPixel2) {
                    return currentHitbox.Intersects(ref other.currentHitbox);
                }

                PixelData p;
                GraphicResource res;
                bool isFacingLeftCurrent;
                int x1, y1, x2, y2, xs, dx, dy;
                if (perPixel1) {
                    p = p1;
                    res = res1;
                    isFacingLeftCurrent = isFacingLeft;

                    x1 = (int)MathF.Max(inter.X, other.currentHitbox.Left);
                    y1 = (int)MathF.Max(inter.Y, other.currentHitbox.Top);
                    x2 = (int)MathF.Min(inter.RightX, other.currentHitbox.Right);
                    y2 = (int)MathF.Min(inter.BottomY, other.currentHitbox.Bottom);

                    xs = (int)box1.X;

                    int frame1 = Math.Min(renderer.CurrentFrame, res.FrameCount - 1);
                    dx = (frame1 % res.Base.FrameConfiguration.X) * res.Base.FrameDimensions.X;
                    dy = (frame1 / res.Base.FrameConfiguration.X) * res.Base.FrameDimensions.Y - (int)box1.Y;
                } else {
                    p = p2;
                    res = res2;
                    isFacingLeftCurrent = other.isFacingLeft;

                    x1 = (int)MathF.Max(inter.X, currentHitbox.Left);
                    y1 = (int)MathF.Max(inter.Y, currentHitbox.Top);
                    x2 = (int)MathF.Min(inter.RightX, currentHitbox.Right);
                    y2 = (int)MathF.Min(inter.BottomY, currentHitbox.Bottom);

                    xs = (int)box2.X;

                    int frame2 = Math.Min(other.renderer.CurrentFrame, res.FrameCount - 1);
                    dx = (frame2 % res.Base.FrameConfiguration.X) * res.Base.FrameDimensions.X;
                    dy = (frame2 / res.Base.FrameConfiguration.X) * res.Base.FrameDimensions.Y - (int)box2.Y;
                }

                // Per-pixel collision check
                for (int i = x1; i < x2; i++) {
                    for (int j = y1; j < y2; j++) {
                        int i1 = i - xs;
                        if (isFacingLeftCurrent) i1 = res.Base.FrameDimensions.X - i1 - 1;

                        if (p[i1 + dx, j + dy].A > 40) {
                            return true;
                        }
                    }
                }

            } else {
                int x1 = (int)inter.X;
                int y1 = (int)inter.Y;
                int x2 = (int)inter.RightX;
                int y2 = (int)inter.BottomY;

                int x1s = (int)box1.X;
                int x2s = (int)box2.X;

                int frame1 = Math.Min(renderer.CurrentFrame, res1.FrameCount - 1);
                int dx1 = (frame1 % res1.Base.FrameConfiguration.X) * res1.Base.FrameDimensions.X;
                int dy1 = (frame1 / res1.Base.FrameConfiguration.X) * res1.Base.FrameDimensions.Y - (int)box1.Y;

                int frame2 = Math.Min(other.renderer.CurrentFrame, res2.FrameCount - 1);
                int dx2 = (frame2 % res2.Base.FrameConfiguration.X) * res2.Base.FrameDimensions.X;
                int dy2 = (frame2 / res2.Base.FrameConfiguration.X) * res2.Base.FrameDimensions.Y - (int)box2.Y;

                // Per-pixel collision check
                for (int i = x1; i < x2; i++) {
                    for (int j = y1; j < y2; j++) {
                        int i1 = i - x1s;
                        if (isFacingLeft) i1 = res1.Base.FrameDimensions.X - i1 - 1;
                        int i2 = i - x2s;
                        if (other.isFacingLeft) i2 = res2.Base.FrameDimensions.X - i2 - 1;

                        if (p1[i1 + dx1, j + dy1].A > 20 && p2[i2 + dx2, j + dy2].A > 20) {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool IsCollidingWithAngled(ActorBase other)
        {
            GraphicResource res1 = (currentTransitionState != AnimState.Idle ? currentTransition : currentAnimation);
            GraphicResource res2 = (other.currentTransitionState != AnimState.Idle ? other.currentTransition : other.currentAnimation);

            PixelData p1 = res1?.Material.Res?.MainTexture.Res?.BasePixmap.Res.PixelData?[0];
            PixelData p2 = res2?.Material.Res?.MainTexture.Res?.BasePixmap.Res.PixelData?[0];
            if (p1 == null || p2 == null) {
                return false;
            }

            Matrix4 transform1 =
                Matrix4.CreateTranslation(new Vector3(-res1.Base.Hotspot.X, -res1.Base.Hotspot.Y, 0f));
            if (isFacingLeft)
                transform1 *= Matrix4.CreateScale(-1f, 1f, 1f);
            transform1 *= Matrix4.CreateRotationZ(Transform.Angle) *
                Matrix4.CreateTranslation(Transform.Pos);

            Matrix4 transform2 =
                Matrix4.CreateTranslation(new Vector3(-res2.Base.Hotspot.X, -res2.Base.Hotspot.Y, 0f));
            if (other.isFacingLeft)
                transform2 *= Matrix4.CreateScale(-1f, 1f, 1f);
            transform2 *= Matrix4.CreateRotationZ(other.Transform.Angle) *
                Matrix4.CreateTranslation(other.Transform.Pos);

            int width1 = res1.Base.FrameDimensions.X;
            int height1 = res1.Base.FrameDimensions.Y;
            int width2 = res2.Base.FrameDimensions.X;
            int height2 = res2.Base.FrameDimensions.Y;

            // Bounding Box intersection
            Hitbox box1, box2;
            {
                Vector2 tl = Vector2.Transform(Vector2.Zero, transform1);
                Vector2 tr = Vector2.Transform(new Vector2(width1, 0f), transform1);
                Vector2 bl = Vector2.Transform(new Vector2(0f, height1), transform1);
                Vector2 br = Vector2.Transform(new Vector2(width1, height1), transform1);

                float minX = MathF.Min(tl.X, tr.X, bl.X, br.X);
                float minY = MathF.Min(tl.Y, tr.Y, bl.Y, br.Y);
                float maxX = MathF.Max(tl.X, tr.X, bl.X, br.X);
                float maxY = MathF.Max(tl.Y, tr.Y, bl.Y, br.Y);

                box1 = new Hitbox(
                    MathF.Floor(minX),
                    MathF.Floor(minY),
                    MathF.Ceiling(maxX),
                    MathF.Ceiling(maxY));
            }
            {
                Vector2 tl = Vector2.Transform(Vector2.Zero, transform2);
                Vector2 tr = Vector2.Transform(new Vector2(width2, 0f), transform2);
                Vector2 bl = Vector2.Transform(new Vector2(0f, height2), transform2);
                Vector2 br = Vector2.Transform(new Vector2(width2, height2), transform2);

                float minX = MathF.Min(tl.X, tr.X, bl.X, br.X);
                float minY = MathF.Min(tl.Y, tr.Y, bl.Y, br.Y);
                float maxX = MathF.Max(tl.X, tr.X, bl.X, br.X);
                float maxY = MathF.Max(tl.Y, tr.Y, bl.Y, br.Y);

                box2 = new Hitbox(
                    MathF.Floor(minX),
                    MathF.Floor(minY),
                    MathF.Ceiling(maxX),
                    MathF.Ceiling(maxY));
            }

            //Hud.ShowDebugRect(new Rect(box1.Left, box1.Top, box1.Right - box1.Left, box1.Bottom - box1.Top));
            //Hud.ShowDebugRect(new Rect(box2.Left, box2.Top, box2.Right - box2.Left, box2.Bottom - box2.Top));

            if (!box1.Intersects(ref box2)) {
                return false;
            }

            // Per-pixel collision check
            Matrix4 transformAToB = transform1 * Matrix4.Invert(transform2);

            // TransformNormal with [1, 0] and [0, 1] vectors
            Vector2 stepX = new Vector2(transformAToB.M11, transformAToB.M12);
            Vector2 stepY = new Vector2(transformAToB.M21, transformAToB.M22);

            Vector2 yPosIn2 = Vector2.Transform(Vector2.Zero, transformAToB);

            int frame1 = MathF.Min(renderer.CurrentFrame, res1.FrameCount - 1);
            int dx1 = (frame1 % res1.Base.FrameConfiguration.X) * res1.Base.FrameDimensions.X;
            int dy1 = (frame1 / res1.Base.FrameConfiguration.X) * res1.Base.FrameDimensions.Y;

            int frame2 = MathF.Min(other.renderer.CurrentFrame, res2.FrameCount - 1);
            int dx2 = (frame2 % res2.Base.FrameConfiguration.X) * res2.Base.FrameDimensions.X;
            int dy2 = (frame2 / res2.Base.FrameConfiguration.X) * res2.Base.FrameDimensions.Y;

            for (int y1 = 0; y1 < height1; y1++) {
                Vector2 posIn2 = yPosIn2;

                for (int x1 = 0; x1 < width1; x1++) {
                    int x2 = (int)MathF.Round(posIn2.X);
                    int y2 = (int)MathF.Round(posIn2.Y);

                    if (x2 >= 0 && x2 < width2 && y2 >= 0 && y2 < height2) {

                        if (p1[x1 + dx1, y1 + dy1].A > 20 && p2[x2 + dx2, y2 + dy2].A > 20) {
                            return true;
                        }
                    }

                    posIn2 += stepX;
                }

                yPosIn2 += stepY;
            }

            return false;
        }

        protected void Illuminate()
        {
            const int lightCount = 20;

            for (int i = 0; i < lightCount; i++) {
                new IlluminateLightPart(1f).Parent = this;
            }
        }

        protected void RequestMetadata(string path)
        {
            Metadata metadata;
            if ((flags & ActorInstantiationFlags.IsCreatedFromEventMap) != 0) {
                metadata = api.RequestMetadataAsync(path);
            } else {
                metadata = api.RequestMetadata(path);
            }

            boundingBox = metadata.BoundingBox;
            availableAnimations = metadata.Graphics;
            availableSounds = metadata.Sounds;
        }

        protected SoundInstance PlaySound(string name, float gain = 1f, float pitch = 1f)
        {
            SoundResource resource;
            if (availableSounds.TryGetValue(name, out resource)) {
                SoundInstance instance = DualityApp.Sound.PlaySound3D(resource.Sound, this);
                // ToDo: Hardcoded volume
                instance.Volume = gain * Settings.SfxVolume;
                instance.Pitch = pitch;

                if (Transform.Pos.Y >= api.WaterLevel) {
                    instance.Lowpass = 0.2f;
                    instance.Pitch *= 0.7f;
                }

                return instance;
            } else {
                return null;
            }
        }

        protected void CreateParticleDebris()
        {
            TileMap tilemap = api.TileMap;
            if (tilemap != null) {
                tilemap.CreateParticleDebris(currentTransitionState != AnimState.Idle ? currentTransition : currentAnimation,
                    Transform.Pos, Vector2.Zero, renderer.CurrentFrame, isFacingLeft);
            }
        }

        protected void CreateSpriteDebris(string identifier, int count)
        {
            TileMap tilemap = api.TileMap;
            if (tilemap != null) {
                GraphicResource res;
                if (availableAnimations.TryGetValue(identifier, out res)) {
                    tilemap.CreateSpriteDebris(res, Transform.Pos, count);
                } else {
                    Console.WriteLine("Can't create sprite debris \"" + identifier + "\" from " + GetType().FullName);
                }
            }
        }

        #region Animations
        private void RefreshAnimation()
        {
            GraphicResource resource = (currentTransitionState != AnimState.Idle ? currentTransition : currentAnimation);

            if (renderer == null) {
                renderer = AddComponent<ActorRenderer>();
                renderer.AnimationFinished = OnAnimationFinished;
                renderer.AlignToPixelGrid = true;
                renderer.Offset = -2000;
            }

            renderer.SharedMaterial = resource.Material;
            renderer.FrameConfiguration = resource.Base.FrameConfiguration;

            if (float.IsInfinity(resource.FrameDuration)) {
                if (resource.FrameCount > 1) {
                    renderer.AnimFirstFrame = resource.FrameOffset + MathF.Rnd.Next(resource.FrameCount);
                } else {
                    renderer.AnimFirstFrame = resource.FrameOffset;
                }

                renderer.AnimLoopMode = ActorRenderer.LoopMode.FixedSingle;
            } else {
                renderer.AnimFirstFrame = resource.FrameOffset;

                renderer.AnimLoopMode = (resource.OnlyOnce ? ActorRenderer.LoopMode.Once : ActorRenderer.LoopMode.Loop);
            }

            renderer.AnimFrameCount = resource.FrameCount;
            renderer.AnimDuration = resource.FrameDuration;
            renderer.Rect = new Rect(
                -resource.Base.Hotspot.X,
                -resource.Base.Hotspot.Y,
                resource.Base.FrameDimensions.X,
                resource.Base.FrameDimensions.Y
            );

            renderer.AnimTime = 0;

            OnAnimationStarted();
        }

        protected void SetAnimation(string identifier)
        {
            currentAnimationState = AnimState.Idle;
            currentAnimation = availableAnimations[identifier];

            // ToDo: Remove this bounding box reduction
            // ToDo: Move bounding box calculation to Import project
            if (boundingBox.X == 0 || boundingBox.Y == 0) {
                boundingBox = currentAnimation.Base.FrameDimensions - new Point2(4, 0);
            }

            RefreshAnimation();
        }

        protected bool SetAnimation(AnimState state)
        {
            if (currentTransitionState != AnimState.Idle && !currentTransitionCancellable) {
                return false;
            }

            if (currentAnimation?.State != null && currentAnimation.State.Contains(state)) {
                currentAnimationState = state;
                return false;
            }

            List<GraphicResource> candidates = FindAnimationCandidates(state);
            if (candidates.Count == 0) {
                return false;
            } else {
                if (currentTransitionState != AnimState.Idle) {
                    currentTransitionState = AnimState.Idle;

                    if (currentTransitionCallback != null) {
                        Action oldCallback = currentTransitionCallback;
                        currentTransitionCallback = null;
                        oldCallback();
                    }
                }

                currentAnimationState = state;
                currentAnimation = candidates[MathF.Rnd.Next() % candidates.Count];

                if (boundingBox.X == 0 || boundingBox.Y == 0) {
                    boundingBox = currentAnimation.Base.FrameDimensions - new Point2(2, 2);
                }

                RefreshAnimation();
                return true;
            }
        }

        protected bool SetTransition(AnimState state, bool cancellable, Action callback = null)
        {
            List<GraphicResource> candidates = FindAnimationCandidates(state);
            if (candidates.Count == 0) {
                // ToDo: Cancel previous transition here?
                if (callback != null) {
                    callback();
                }

                return false;
            } else {
                if (currentTransitionCallback != null) {
                    Action oldCallback = currentTransitionCallback;
                    currentTransitionCallback = null;
                    oldCallback();
                }

                currentTransitionCallback = callback;

                currentTransitionState = state;
                currentTransitionCancellable = cancellable;
                currentTransition = candidates[0];

                RefreshAnimation();
                return true;
            }
        }

        protected void CancelTransition()
        {
            if (currentTransitionState != AnimState.Idle && currentTransitionCancellable) {
                if (currentTransitionCallback != null) {
                    Action oldCallback = currentTransitionCallback;
                    currentTransitionCallback = null;
                    oldCallback();
                }

                currentTransitionState = AnimState.Idle;

                RefreshAnimation();
            }
        }

        protected virtual void OnAnimationStarted()
        {
            // Could be overriden
        }

        protected virtual void OnAnimationFinished()
        {
            if (currentTransitionState != AnimState.Idle) {
                currentTransitionState = AnimState.Idle;

                RefreshAnimation();

                if (currentTransitionCallback != null) {
                    Action oldCallback = currentTransitionCallback;
                    currentTransitionCallback = null;
                    oldCallback();
                }
            }
        }

        protected List<GraphicResource> FindAnimationCandidates(AnimState state)
        {
            List<GraphicResource> candidates = new List<GraphicResource>();
            foreach (var animation in availableAnimations) {
                if (animation.Value.State != null && animation.Value.State.Contains(state)) {
                    candidates.Add(animation.Value);
                }
            }
            return candidates;
        }
        #endregion

        private class LocalController : Component, ICmpUpdatable, ICmpInitializable
        {
            private readonly ActorBase actor;

            public LocalController(ActorBase actor)
            {
                this.actor = actor;
            }

            public void OnInit(InitContext context)
            {
                //
            }

            public void OnShutdown(ShutdownContext context)
            {
                //
                actor.OnDeactivated(context);
            }

            void ICmpUpdatable.OnUpdate()
            {
                actor.OnUpdate();
            }
        }
    }
}