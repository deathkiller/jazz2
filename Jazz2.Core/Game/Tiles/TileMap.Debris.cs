using Duality;
using Duality.Drawing;
using Duality.Resources;
using Jazz2.Game.Structs;
using Jazz2.Game.UI;

namespace Jazz2.Game.Tiles
{
    partial class TileMap
    {
        public enum DebrisCollisionAction
        {
            None,
            Disappear,
            Bounce
        }

        public struct DestructibleDebris
        {
            public Vector3 Pos;
            public Vector2 Size;
            public Vector2 Speed;
            public Vector2 Acceleration;

            public float Scale;
            public float ScaleSpeed;

            public float Angle;
            public float AngleSpeed;

            public float Alpha;
            public float AlphaSpeed;

            public float Time;

            public Material Material;
            public Rect MaterialOffset;

            public DebrisCollisionAction CollisionAction;
        }

        private readonly RawList<DestructibleDebris> debrisList = new RawList<DestructibleDebris>();
        private VertexC1P3T2[] debrisVertices;

        public void CreateDebris(DestructibleDebris debris)
        {
            if (debris.CollisionAction == DebrisCollisionAction.Disappear && debris.Pos.Z >= levelLayout[sprLayerIndex].Depth /*&& !IsTileEmpty((int)debris.Pos.X / 32, (int)debris.Pos.Y / 32)*/) {
                int x = (int)debris.Pos.X / 32;
                int y = (int)debris.Pos.Y / 32;
                if (x < 0 || y < 0 || x >= levelWidth || y >= levelHeight) {
                    return;
                }

                int idx = levelLayout[sprLayerIndex].Layout[x + y * levelWidth].TileID;
                if (levelLayout[sprLayerIndex].Layout[x + y * levelWidth].IsAnimated) {
                    idx = animatedTiles[idx].CurrentTile.TileID;
                }

                if (tileset.IsTileFilled(idx)) {
                    return;
                }

                if (sprLayerIndex + 1 < levelLayout.Count && levelLayout[sprLayerIndex + 1].SpeedX == 1f && levelLayout[sprLayerIndex + 1].SpeedY == 1f) {
                    idx = levelLayout[sprLayerIndex + 1].Layout[x + y * levelWidth].TileID;
                    if (levelLayout[sprLayerIndex + 1].Layout[x + y * levelWidth].IsAnimated) {
                        idx = animatedTiles[idx].CurrentTile.TileID;
                    }

                    if (tileset.IsTileFilled(idx)) {
                        return;
                    }
                }
            }

            debrisList.Add(debris);
        }

        public void CreateTileDebris(int tileID, int x, int y)
        {
            float[] speedMultiplier = { -2, 2, -1, 1 };
            int quarterSize = Tileset.TileSize / 2;
            float z = levelLayout[sprLayerIndex].Depth - 80f;

            int tx = (tileID % tileset.TilesPerRow) * tileset.TileSize;
            int ty = (tileID / tileset.TilesPerRow) * tileset.TileSize;

            Material material = tileset.Material.Res;
            Texture texture = material.MainTexture.Res;

            for (int i = 0; i < 4; i++) {
                debrisList.Add(new DestructibleDebris {
                    Pos = new Vector3(x * 32 + (i % 2) * quarterSize, y * 32 + (i / 2) * quarterSize, z),
                    Size = new Vector2(quarterSize, quarterSize),
                    Speed = new Vector2(speedMultiplier[i] * MathF.Rnd.NextFloat(0.8f, 1.2f), -4f * MathF.Rnd.NextFloat(0.8f, 1.2f)),
                    Acceleration = new Vector2(0f, 0.3f),

                    Scale = 1f,
                    ScaleSpeed = MathF.Rnd.NextFloat(-0.01f, -0.002f),
                    AngleSpeed = speedMultiplier[i] * MathF.Rnd.NextFloat(0f, 0.014f),

                    Alpha = 1f,
                    AlphaSpeed = -0.01f,

                    Time = 120f,

                    Material = material,
                    MaterialOffset = new Rect(
                        (tx + (i % 2) * quarterSize) * texture.UVRatio.X / texture.ContentWidth,
                        (ty + (i / 2) * quarterSize) * texture.UVRatio.Y / texture.ContentHeight,
                        quarterSize * texture.UVRatio.X / texture.ContentWidth,
                        quarterSize * texture.UVRatio.Y / texture.ContentHeight
                    )
                });
            }
        }

        public void CreateParticleDebris(GraphicResource res, Vector3 pos, Vector2 force, int currentFrame, bool isFacingLeft)
        {
            const int debrisSize = 3;

            Material material = res.Material.Res;
            Texture texture = material.MainTexture.Res;

            float x = pos.X - res.Base.Hotspot.X;
            float y = pos.Y - res.Base.Hotspot.Y;

            for (int fx = 0; fx < res.Base.FrameDimensions.X; fx += debrisSize + 1) {
                for (int fy = 0; fy < res.Base.FrameDimensions.Y; fy += debrisSize + 1) {
                    float currentSize = debrisSize * MathF.Rnd.NextFloat(0.2f, 1.1f);
                    debrisList.Add(new DestructibleDebris {
                        Pos = new Vector3(x + (isFacingLeft ? res.Base.FrameDimensions.X - fx : fx), y + fy, pos.Z),
                        Size = new Vector2(currentSize /** (isFacingLeft ? -1f : 1f)*/, currentSize),
                        Speed = new Vector2(force.X + ((fx - res.Base.FrameDimensions.X / 2) + MathF.Rnd.NextFloat(-2f, 2f)) * (isFacingLeft ? -1f : 1f) * MathF.Rnd.NextFloat(2f, 8f) / res.Base.FrameDimensions.X,
                            force.Y - 1f * MathF.Rnd.NextFloat(2.2f, 4f)),
                        Acceleration = new Vector2(0f, 0.2f),

                        Scale = 1f,
                        Alpha = 1f,
                        AlphaSpeed = -0.002f,

                        Time = 320f,

                        Material = material,
                        MaterialOffset = new Rect(
                            (((float)(currentFrame % res.Base.FrameConfiguration.X) / res.Base.FrameConfiguration.X) + ((float)fx / texture.ContentWidth)) * texture.UVRatio.X,
                            (((float)(currentFrame / res.Base.FrameConfiguration.X) / res.Base.FrameConfiguration.Y) + ((float)fy / texture.ContentHeight)) * texture.UVRatio.Y,
                            (currentSize * texture.UVRatio.X / texture.ContentWidth),
                            (currentSize * texture.UVRatio.Y / texture.ContentHeight)
                        ),

                        CollisionAction = DebrisCollisionAction.Bounce
                    });
                }
            }
        }

        public void CreateSpriteDebris(GraphicResource res, Vector3 pos, int count)
        {
            Material material = res.Material.Res;
            Texture texture = material.MainTexture.Res;

            float x = pos.X - res.Base.Hotspot.X;
            float y = pos.Y - res.Base.Hotspot.Y;

            for (int i = 0; i < count; i++) {
                float speedX = MathF.Rnd.NextFloat(-1f, 1f) * MathF.Rnd.NextFloat(0.2f, 0.8f) * count;
                debrisList.Add(new DestructibleDebris {
                    Pos = new Vector3(x, y, pos.Z),
                    Size = res.Base.FrameDimensions,
                    Speed = new Vector2(speedX, -1f * MathF.Rnd.NextFloat(2.2f, 4f)),
                    Acceleration = new Vector2(0f, 0.2f),

                    Scale = 1f,
                    ScaleSpeed = -0.002f,
                    Angle = MathF.Rnd.NextFloat(MathF.TwoPi),
                    AngleSpeed = speedX * 0.02f,
                    Alpha = 1f,
                    AlphaSpeed = -0.002f,

                    Time = 560f,

                    Material = material,
                    MaterialOffset = texture.LookupAtlas(res.FrameOffset + MathF.Rnd.Next(res.FrameCount)),

                    CollisionAction = DebrisCollisionAction.Bounce
                });
            }
        }

        private void UpdateDebris(float timeMult)
        {
            for (int i = 0; i < debrisList.Count; i++) {
                ref DestructibleDebris debris = ref debrisList.Data[i];

                debris.Time -= timeMult;
                if (debris.Scale <= 0f || debris.Alpha <= 0f) {
                    debrisList.RemoveAtFast(i);
                    i--;
                    continue;
                }
                if (debris.Time <= 0f) {
                    debris.AlphaSpeed = -MathF.Min(0.02f, debris.Alpha);
                }

                if (debris.CollisionAction != DebrisCollisionAction.None) {
                    // Debris should collide with tilemap
                    float nx = debris.Pos.X + debris.Speed.X * timeMult;
                    float ny = debris.Pos.Y + debris.Speed.Y * timeMult;
                    Hitbox hitbox = new Hitbox(nx - 1, ny - 1, nx + 1, ny + 1);
                    if (IsTileEmpty(ref hitbox, true)) {
                        // Nothing...
                    } else if (debris.CollisionAction == DebrisCollisionAction.Disappear) {
                        debris.ScaleSpeed = -0.02f;
                        debris.AlphaSpeed = -0.006f;
                        debris.Speed = Vector2.Zero;
                        debris.Acceleration = Vector2.Zero;
                    } else {
                        // Place us to the ground only if no horizontal movement was
                        // involved (this prevents speeds resetting if the actor
                        // collides with a wall from the side while in the air)
                        hitbox = new Hitbox(nx - 1, debris.Pos.Y - 1, nx + 1, debris.Pos.Y + 1);
                        if (IsTileEmpty(ref hitbox, true)) {
                            if (debris.Speed.Y > 0f) {
                                debris.Speed.Y = -(0.8f/*elasticity*/ * debris.Speed.Y);
                                //OnHitFloorHook();
                            } else {
                                debris.Speed.Y = 0;
                                //OnHitCeilingHook();
                            }
                        }

                        // If the actor didn't move all the way horizontally,
                        // it hit a wall (or was already touching it)
                        hitbox = new Hitbox(debris.Pos.X - 1, ny - 1, debris.Pos.X + 1, ny + 1);
                        if (IsTileEmpty(ref hitbox, true)) {
                            debris.Speed.X = -(0.8f/*elasticity*/ * debris.Speed.X);
                            debris.AngleSpeed = -(0.8f/*elasticity*/ * debris.AngleSpeed);
                            //OnHitWallHook();
                        }
                    }
                }

                debris.Pos.X += debris.Speed.X * timeMult;
                debris.Pos.Y += debris.Speed.Y * timeMult;

                if (debris.Acceleration.X != 0f) {
                    debris.Speed.X = MathF.Min(debris.Speed.X + debris.Acceleration.X * timeMult, 10f);
                }
                if (debris.Acceleration.Y != 0f) {
                    debris.Speed.Y = MathF.Min(debris.Speed.Y + debris.Acceleration.Y * timeMult, 10f);
                }

                debris.Scale += debris.ScaleSpeed * timeMult;
                debris.Angle += debris.AngleSpeed * timeMult;
                debris.Alpha += debris.AlphaSpeed * timeMult;
            }
        }

        private void DrawDebris(IDrawDevice device)
        {
            if (debrisList.Count == 0) {
                return;
            }

            Material material = debrisList.Data[0].Material;
            ColorRgba mainColor = ColorRgba.White;

            int neededVertices = debrisList.Count * 4;
            if (debrisVertices == null || debrisVertices.Length < neededVertices) {
                debrisVertices = new VertexC1P3T2[neededVertices + 80];
            }

            int vertexOffset = 0;
            int vertexBaseIndex = 0;

            for (int i = 0; i < debrisList.Count; i++) {
                ref DestructibleDebris debris = ref debrisList.Data[i];

                if (material != debris.Material) {
                    device.AddVertices(
                        material,
                        VertexMode.Quads,
                        debrisVertices,
                        vertexOffset,
                        vertexBaseIndex);

                    vertexOffset += vertexBaseIndex;
                    vertexBaseIndex = 0;

                    material = debris.Material;
                }

                mainColor.A = (byte)(debris.Alpha * 255);

                Vector3 renderPos = debris.Pos;
                float scale = 1f;
                device.PreprocessCoords(ref renderPos, ref scale);

                Vector2 xDot, yDot;
                MathF.GetTransformDotVec(debris.Angle, scale, out xDot, out yDot);

                Vector2 edge1 = new Vector2(0, 0);
                Vector2 edge2 = new Vector2(0, debris.Size.Y * debris.Scale);
                Vector2 edge3 = new Vector2(debris.Size.Y * debris.Scale, debris.Size.Y * debris.Scale);
                Vector2 edge4 = new Vector2(debris.Size.X * debris.Scale, 0);
                MathF.TransformDotVec(ref edge1, ref xDot, ref yDot);
                MathF.TransformDotVec(ref edge2, ref xDot, ref yDot);
                MathF.TransformDotVec(ref edge3, ref xDot, ref yDot);
                MathF.TransformDotVec(ref edge4, ref xDot, ref yDot);
                edge1 += new Vector2(MathF.Round(renderPos.X), MathF.Round(renderPos.Y));
                edge2 += new Vector2(MathF.Round(renderPos.X), MathF.Round(renderPos.Y));
                edge3 += new Vector2(MathF.Round(renderPos.X), MathF.Round(renderPos.Y));
                edge4 += new Vector2(MathF.Round(renderPos.X), MathF.Round(renderPos.Y));

                //renderPos.X = MathF.Round(renderPos.X);
                //renderPos.Y = MathF.Round(renderPos.Y);
                //if (MathF.RoundToInt(device.TargetSize.X) != (MathF.RoundToInt(device.TargetSize.X) / 2) * 2) {
                //    renderPos.X += 0.5f;
                //}
                //if (MathF.RoundToInt(device.TargetSize.Y) != (MathF.RoundToInt(device.TargetSize.Y) / 2) * 2) {
                //    renderPos.Y += 0.5f;
                //}

                int vertexIndex = vertexBaseIndex + vertexOffset;

                debrisVertices[vertexIndex].Pos.Xy = edge1;
                debrisVertices[vertexIndex].Pos.Z = renderPos.Z;
                debrisVertices[vertexIndex].TexCoord = debris.MaterialOffset.TopLeft;
                debrisVertices[vertexIndex].Color = mainColor;

                debrisVertices[vertexIndex + 1].Pos.Xy = edge2;
                debrisVertices[vertexIndex + 1].Pos.Z = renderPos.Z;
                debrisVertices[vertexIndex + 1].TexCoord = debris.MaterialOffset.BottomLeft;
                debrisVertices[vertexIndex + 1].Color = mainColor;

                debrisVertices[vertexIndex + 2].Pos.Xy = edge3;
                debrisVertices[vertexIndex + 2].Pos.Z = renderPos.Z;
                debrisVertices[vertexIndex + 2].TexCoord = debris.MaterialOffset.BottomRight;
                debrisVertices[vertexIndex + 2].Color = mainColor;

                debrisVertices[vertexIndex + 3].Pos.Xy = edge4;
                debrisVertices[vertexIndex + 3].Pos.Z = renderPos.Z;
                debrisVertices[vertexIndex + 3].TexCoord = debris.MaterialOffset.TopRight;
                debrisVertices[vertexIndex + 3].Color = mainColor;

                vertexBaseIndex += 4;
            }

            // Submit all the vertices as one draw batch
            device.AddVertices(
                material,
                VertexMode.Quads,
                debrisVertices,
                vertexOffset,
                vertexBaseIndex);

            Hud.ShowDebugText("- Debris: " + debrisList.Count + " (" + neededVertices + "/" + debrisVertices.Length + ")");
        }
    }
}