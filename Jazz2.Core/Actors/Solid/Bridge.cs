using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Duality;
using Jazz2.Game;
using Jazz2.Game.Collisions;
using Jazz2.Game.Events;
using MathF = Duality.MathF;

namespace Jazz2.Actors.Solid
{
    public class Bridge : ActorBase
    {
        public enum BridgeType
        {
            Rope = 0,
            Stone = 1,
            Vine = 2,
            StoneRed = 3,
            Log = 4,
            Gem = 5,
            Lab = 6
        }

        private static readonly int[][] PieceWidths = {
            new[] { 13, 13, 10, 13, 13, 12, 11 },
            new[] { 15, 9, 10, 9, 15, 9, 15 },
            new[] { 7, 7, 7, 7, 10, 7, 7, 7, 7 },
            new[] { 10, 11, 11, 12 },
            new[] { 13, 13, 13 },
            new[] { 14 },
            new[] { 14 }
        };

        private float originalY;
        private BridgeType bridgeType;
        private int bridgeWidth;
        private float heightFactor;
        private List<Piece> bridgePieces;

        private List<ActorBase> collisions = new List<ActorBase>();
        private Player lastPlayer;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            bridgeWidth = details.Params[0];
            bridgeType = (BridgeType)details.Params[1];
            if (bridgeType > BridgeType.Lab) {
                bridgeType = BridgeType.Rope;
            }

            int toughness = details.Params[2];
            heightFactor = MathF.Sqrt((16 - toughness) * bridgeWidth) * 4f;

            // Request metadata here to allow async loading
            await RequestMetadataAsync("Bridge/" + bridgeType.ToString("G"));

            Vector3 pos = Transform.Pos;
            originalY = pos.Y - 6;

            bridgePieces = new List<Piece>();

            int[] widthList = PieceWidths[(int)bridgeType];

            int widthCovered = widthList[0] / 2;
            for (int i = 0; (widthCovered <= bridgeWidth * 16 + 6) || (i * 16 < bridgeWidth); i++) {
                Piece piece = new Piece();
                piece.OnActivated(new ActorActivationDetails {
                    Api = api,
                    Pos = new Vector3(pos.X + widthCovered - 16, pos.Y - 20, LevelHandler.MainPlaneZ + 10),
                    Params = new[] { (ushort)bridgeType, (ushort)i }
                });
                api.AddActor(piece);

                bridgePieces.Add(piece);

                widthCovered += (widthList[i % widthList.Length] + widthList[(i + 1) % widthList.Length]) / 2;
            }

            collisionFlags = CollisionFlags.CollideWithOtherActors | CollisionFlags.SkipPerPixelCollisions;
        }

        public override bool OnTileDeactivate(int tx1, int ty1, int tx2, int ty2)
        {
            if ((flags & ActorInstantiationFlags.IsCreatedFromEventMap) != 0) {
                if (originTile.X < tx1 || originTile.Y < ty1 || originTile.X > tx2 || originTile.Y > ty2) {
                    EventMap events = api.EventMap;
                    if (events != null) {
                        events.Deactivate(originTile.X, originTile.Y);
                    }

                    for (int i = 0; i < bridgePieces.Count; ++i) {
                        api.RemoveActor(bridgePieces[i]);
                    }
                    api.RemoveActor(this);
                    return true;
                }
            }
            return false;
        }

        protected override void OnUpdateHitbox()
        {
            Vector3 pos = Transform.Pos;
            AABBInner = new AABB(pos.X - 16, pos.Y - 10, pos.X - 16 + bridgeWidth * 16, pos.Y + 16);
        }

        protected override void OnUpdate()
        {
            collisions.Clear();

            api.FindCollisionActorsByAABB(this, AABBInner, ResolveCollisions);

            Vector3 pos = Transform.Pos;

            bool found = false;
            foreach (ActorBase collision in collisions) {
                // ToDo: This code only works with one player
                Player player = collision as Player;
                if (player != null) {
                    if (player != lastPlayer) {
                        if (player.Speed.Y < -0.5f) {
                            continue;
                        }

                        lastPlayer = player;
                    }

                    found = true;
                    Vector3 coords = player.Transform.Pos;
                    int length = bridgePieces.Count;

                    // This marks which bridge piece is under the player and should be positioned
                    // lower than any other piece of the bridge.
                    float lowest = (coords.X - pos.X) / (bridgeWidth * 16f) * length;

                    // This marks the maximum drop in height.
                    // At the middle of the bridge, this is purely the height factor,
                    // which is simply (16 - bridge toughness) multiplied by the length of the bridge.
                    // At other points, the height is scaled by an (arbitrarily chosen) power that
                    // gives a nice curve.
                    // Additionally, the drop is reduced based on the player position so that the
                    // bridge seems to bend somewhat realistically instead of snapping from one position
                    // to another.
                    float drop = Math.Max(0, Math.Min(coords.Y - pos.Y + 32, (1f - MathF.Pow(Math.Abs(2f * lowest / length - 1f), 0.8f)) * heightFactor));

                    pos.Y = Math.Min(originalY + drop, Math.Max(originalY, coords.Y));

                    Transform.Pos = pos;

                    // Update the position of each bridge piece.
                    for (int j = 0; j < length; ++j) {
                        Piece piece = bridgePieces[j];
                        coords = piece.Transform.Pos;

                        if (lowest > 0 && lowest < length) {
                            float dropPiece;
                            if (j <= lowest) {
                                dropPiece = MathF.Pow(j / lowest, 0.6f) * drop;

                                piece.Transform.Angle = dropPiece * 0.006f;
                            } else {
                                dropPiece = MathF.Pow((length - 1 - j) / (length - 1 - lowest), 0.6f) * drop;

                                piece.Transform.Angle = -dropPiece * 0.006f;
                            }
                            coords.Y = originalY + dropPiece;
                        } else {
                            coords.Y = originalY;

                            piece.Transform.Angle = 0f;
                        }

                        piece.Transform.Pos = coords;
                    }
                }
            }

            if (!found) {
                // The player was not touching the bridge, so reset all pieces to the default height.
                for (int j = 0; j < bridgePieces.Count; ++j) {
                    Vector3 coords = bridgePieces[j].Transform.Pos;
                    coords.Y = originalY;
                    bridgePieces[j].Transform.Pos = coords;
                    bridgePieces[j].Transform.Angle = 0f;
                }

                pos.Y = originalY;
                Transform.Pos = pos;

                lastPlayer = null;
            }
        }

        private bool ResolveCollisions(ActorBase actor)
        {
            collisions.Add(actor);
            return true;
        }

        public class Piece : SolidObjectBase
        {
            protected override async Task OnActivatedAsync(ActorActivationDetails details)
            {
                BridgeType type = (BridgeType)details.Params[0];

                canBeFrozen = false;

                await RequestMetadataAsync("Bridge/" + type.ToString("G"));
                SetAnimation("Piece");

                int variations = currentAnimation.FrameCount;
                if (variations > 0) {
                    ushort idx = details.Params[1];
                    renderer.AnimFirstFrame = idx % variations;
                }

                collisionFlags = CollisionFlags.CollideWithOtherActors | CollisionFlags.IsSolidObject | CollisionFlags.SkipPerPixelCollisions;

                IsOneWay = true;
            }

            public override bool OnTileDeactivate(int tx1, int ty1, int tx2, int ty2)
            {
                // Removal of bridge pieces is handled by the bridge
                return false;
            }

            protected override void OnUpdate()
            {
                // The bridge piece is controlled by the bridge
                //base.OnUpdate();

                OnUpdateHitbox();
            }

            protected override void OnUpdateHitbox()
            {
                UpdateHitbox(20, 10);
            }
        }
    }
}