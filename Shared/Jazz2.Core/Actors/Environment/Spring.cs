﻿using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Collisions;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Environment
{
    // ToDo: Implement keepXSpeed
    // ToDo: Implement keepYSpeed
    // ToDo: Implement delay
    // ToDo: Implement frozen

    public class Spring : ActorBase
    {
        private ushort type;
        private ushort orientation;
        private float strength;
        private bool keepSpeedX, keepSpeedY;
        //private ushort delay;
        //private bool frozen;

        private float cooldown;

        public bool KeepSpeedX => keepSpeedX;
        public bool KeepSpeedY => keepSpeedY;

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Object/Spring");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new Spring();
            actor.OnActivated(details);
            return actor;
        }

        private Spring()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            type = details.Params[0];
            orientation = details.Params[1];
            keepSpeedX = (details.Params[2] != 0);
            keepSpeedY = (details.Params[3] != 0);
            //delay = details.Params[4];
            //frozen = (details.Params[5] != 0);

            CollisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            Vector3 pos = Transform.Pos;

            await RequestMetadataAsync("Object/Spring");

            Vector2 tileCorner = new Vector2((int)(pos.X / 32) * 32, (int)(pos.Y / 32) * 32);
            if (orientation > 3) {
                // JJ2 horizontal springs held no data about which way they were facing.
                // For compatibility, the level converter sets their orientation to 5, which is interpreted here.
                AABB aabb = new AABB(pos.X + 6, pos.Y - 2, pos.X + 22, pos.Y + 2);
                orientation = (ushort)(levelHandler.TileMap.IsTileEmpty(ref aabb, false) != (orientation == 5) ? 1 : 3);
            }

            int orientationBit = 0;
            switch (orientation) {
                case 0: // Bottom
                    MoveInstantly(new Vector2(tileCorner.X + 16, tileCorner.Y + 8), MoveType.Absolute, true);
                    break;
                case 1: // Right
                    MoveInstantly(new Vector2(tileCorner.X + 16, tileCorner.Y + 16), MoveType.Absolute, true);
                    orientationBit = 1;
                    CollisionFlags &= ~CollisionFlags.ApplyGravitation;
                    break;
                case 2: // Top
                    MoveInstantly(new Vector2(tileCorner.X + 16, tileCorner.Y + 8), MoveType.Absolute, true);
                    orientationBit = 2;
                    CollisionFlags &= ~CollisionFlags.ApplyGravitation;
                    break;
                case 3: // Left
                    MoveInstantly(new Vector2(tileCorner.X + 16, tileCorner.Y + 16), MoveType.Absolute, true);
                    orientationBit = 1;
                    CollisionFlags &= ~CollisionFlags.ApplyGravitation;
                    IsFacingLeft = true;
                    break;
            }

            // Red starts at 1 in "Object/Spring"
            SetAnimation((AnimState)(((type + 1) << 10) | (orientationBit << 12)));

            if (orientation % 2 == 1) {
                // Horizontal springs all seem to have the same strength.
                // This constant strength gives about the correct amount of horizontal push.
                strength = 9.5f;
            } else {
                // Vertical springs should work as follows:
                // Red spring lifts the player 9 tiles, green 14, and blue 19.
                // Vertical strength currently works differently from horizontal, that explains
                // the otherwise inexplicable difference of scale between the two types.
                switch (type) {
                    case 0: // Red
                        strength = 1.25f;
                        break;
                    case 1: // Green
                        strength = 1.50f;
                        break;
                    case 2: // Blue
                        strength = 1.65f;
                        break;
                }
            }

            if ((CollisionFlags & CollisionFlags.ApplyGravitation) != 0) {
                OnUpdateHitbox();

                // Apply instant gravitation
                int i = 10;
                while (i-- > 0 && MoveInstantly(new Vector2(0f, 4f), MoveType.Relative)) {
                    // Nothing to do...
                }
                while (i-- > 0 && MoveInstantly(new Vector2(0f, 1f), MoveType.Relative)) {
                    // Nothing to do...
                }
            }
        }

        public override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            if (cooldown > 0f) {
                cooldown -= timeMult;
            }
        }

        protected override void OnUpdateHitbox()
        {
            Vector3 pos = Transform.Pos;
            switch (orientation) {
                case 1: // Right
                    AABBInner = new AABB(pos.X - 8, pos.Y - 10, pos.X, pos.Y + 10);
                    break;
                case 3: // Left
                    AABBInner = new AABB(pos.X, pos.Y - 10, pos.X + 8, pos.Y + 10);
                    break;
                default:
                case 0: // Bottom
                case 2: // Top
                    AABBInner = new AABB(pos.X - 10, pos.Y, pos.X + 10, pos.Y + 8);
                    break;
            }
        }

        public Vector2 Activate()
        {
            if (cooldown > 0f || frozenTimeLeft > 0f) {
                return Vector2.Zero;
            }

            cooldown = 6f;

            SetTransition(currentAnimationState | (AnimState)0x200, false);
            switch (orientation) {
                case 0: // Bottom
                    PlaySound("Vertical");
                    return new Vector2(0, -strength);
                case 2: // Top
                    PlaySound("VerticalReversed");
                    return new Vector2(0, strength);
                case 1: // Right
                case 3: // Left
                    PlaySound("Horizontal");
                    return new Vector2(strength * (orientation == 1 ? 1 : -1), 0);
                default:
                    return Vector2.Zero;

            }
        }
    }
}