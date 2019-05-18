using Duality;
using Duality.Components;
using Jazz2.Game;
using Jazz2.Game.Collisions;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Solid
{
    public class MovingPlatform : SolidObjectBase
    {
        private enum PlatformType
        {
            CarrotusFruit = 1,
            Ball = 2,
            CarrotusGrass = 3,
            Lab = 4,
            Sonic = 5,
            Spike = 6,

            SpikeBall = 7,
        }

        private const float BaseCycleFrames = 550f;

        private PlatformType type;
        private float speed;
        private int length;
        private bool isSwing;

        private float phase;
        private Vector3 originPos, lastPos;
        private ChainPiece[] pieces;

        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

            originPos = Transform.Pos;
            lastPos = originPos;

            type = (PlatformType)details.Params[0];
            length = details.Params[3];
            speed = (details.Params[2] > short.MaxValue ? -(ushort.MaxValue - details.Params[2]) : details.Params[2]) * 0.78f;
            ushort sync = details.Params[1];
            isSwing = details.Params[4] != 0;

            phase = (BaseCycleFrames - (float)(Time.GameTimer.TotalMilliseconds % BaseCycleFrames - sync * 175) * speed) % BaseCycleFrames;

            collisionFlags = CollisionFlags.CollideWithOtherActors | CollisionFlags.IsSolidObject;
            IsOneWay = true;
            canBeFrozen = false;

            RequestMetadata("MovingPlatform/" + type.ToString("G"));
            SetAnimation("Platform");

            pieces = new ChainPiece[length];
            for (int i = 0; i < length; i++) {
                pieces[i] = new ChainPiece(api, originPos + new Vector3(0f, 0f, 4f), type, i);
                api.AddActor(pieces[i]);
            }
        }

        protected override void OnDeactivated(Component.ShutdownContext context)
        {
            for (int i = 0; i < length; i++) {
                api.RemoveActor(pieces[i]);
            }
        }

        protected override void OnUpdate()
        {
            if (length > 0) {
                lastPos = Transform.Pos;

                phase -= speed * Time.TimeMult;
                if (phase < 0f) {
                    phase += BaseCycleFrames;
                }

                MoveInstantly(GetPhasePosition(false, length), MoveType.Absolute, true);

                for (int i = 0; i < length; i++) {
                    pieces[i].Transform.Pos = new Vector3(GetPhasePosition(false, i), pieces[i].Transform.Pos.Z);
                }

                AABB aabb = AABBInner;
                aabb.LowerBound.Y -= 2;

                if (type != PlatformType.SpikeBall) {
                    foreach (Player player in api.GetCollidingPlayers(aabb)) {
                        player.SetCarryingPlatform(this);
                    }

                    if (type == PlatformType.Spike) {
                        aabb.LowerBound.Y += 40;
                        aabb.UpperBound.Y += 40;

                        foreach (Player player in api.GetCollidingPlayers(aabb)) {
                            player.TakeDamage(1, 2);
                        }
                    }
                } else {
                    foreach (Player player in api.GetCollidingPlayers(aabb)) {
                        player.TakeDamage(1, 2);
                    }
                }
            }

            base.OnUpdate();
        }

        protected override void OnUpdateHitbox()
        {
            base.OnUpdateHitbox();

            AABBInner.UpperBound.Y = AABBInner.LowerBound.Y + 10;
        }

        public Vector2 GetLocationDelta()
        {
            return Transform.Pos.Xy - lastPos.Xy;
        }

        public Vector2 GetPhasePosition(bool next, int distance)
        {
            float effectivePhase = phase;

            if (next) {
                effectivePhase -= speed * Time.TimeMult;
            }

            if (isSwing) {
                // Mirror the upper half of the circle motion
                if (effectivePhase > BaseCycleFrames / 2) {
                    effectivePhase = BaseCycleFrames - effectivePhase;
                }

                // Blinn-Wyvill approximation to the raised inverted cosine,
                // easing curve with slower ends and faster middle part
                float i = (effectivePhase / BaseCycleFrames * 2);
                effectivePhase = ((4f / 9f) * MathF.Pow(i, 6) - (17f / 9f) * MathF.Pow(i, 4) + (22f / 9f) * MathF.Pow(i, 2)) * BaseCycleFrames / 2;
            } else if (length > 4) {
                int halfLength = length / 2;
                float shift = MathF.Sqrt(MathF.Max(0f, 1f - ((float)MathF.Abs(distance - halfLength) / halfLength))) * speed * 4f;
                effectivePhase -= shift;
            }

            float multiX = MathF.Cos(effectivePhase / BaseCycleFrames * MathF.TwoPi);
            float multiY = MathF.Sin(effectivePhase / BaseCycleFrames * MathF.TwoPi);

            return new Vector2(
                (int)(originPos.X + multiX * distance * 12),
                (int)(originPos.Y + multiY * distance * 12)
            );
        }

        private class ChainPiece : ActorBase
        {
            private int distance;

            public ChainPiece(ActorApi api, Vector3 pos, PlatformType type, int distance)
            {
                this.api = api;
                this.distance = distance;

                Transform transform = AddComponent<Transform>();
                Transform.Pos = pos;

                collisionFlags = CollisionFlags.ForceDisableCollisions;

                RequestMetadata("MovingPlatform/" + type.ToString("G"));
                SetAnimation("Chain");
            }

            protected override void OnUpdate()
            {
                // Controlled by MovingPlatform
            }
        }
    }
}