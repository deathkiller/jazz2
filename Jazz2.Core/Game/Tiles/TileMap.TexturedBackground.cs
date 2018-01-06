using Duality;
using Duality.Drawing;
using Duality.Resources;
using Jazz2.Game.Structs;

namespace Jazz2.Game.Tiles
{
    partial class TileMap
    {
        private ContentRef<Texture> cachedTexturedBackground;
        private bool cachedTexturedBackgroundAnimated;

        private ContentRef<DrawTechnique> texturedBackgroundShader;

        private void RecreateTexturedBackground(ref TileMapLayer layer)
        {
            int w = layer.LayoutWidth;
            int h = layer.Layout.Length / w;

            cachedTexturedBackgroundAnimated = false;

            Texture renderTarget;
            if (cachedTexturedBackground != null) {
                renderTarget = cachedTexturedBackground.Res;
            } else {
                renderTarget = new Texture(w * 32, h * 32, TextureSizeMode.NonPowerOfTwo, TextureMagFilter.Linear, TextureMinFilter.Linear, TextureWrapMode.Repeat, TextureWrapMode.Repeat);

                switch (layer.BackgroundStyle) {
                    case BackgroundStyle.Sky:
                    default:
                        texturedBackgroundShader = ContentResolver.Current.RequestShader("TexturedBackground");
                        break;

                    case BackgroundStyle.Circle:
                        texturedBackgroundShader = ContentResolver.Current.RequestShader("TexturedBackgroundCircle");
                        break;
                }
            }

            using (DrawDevice device = new DrawDevice()) {
                device.VisibilityMask = VisibilityFlag.AllFlags;
                device.RenderMode = RenderMatrix.ScreenSpace;

                device.Target = new RenderTarget(AAQuality.Off, false, renderTarget);
                device.TargetSize = new Vector2(w * 32, h * 32);
                device.ViewportRect = new Rect(device.TargetSize);

                device.PrepareForDrawcalls();

                Material material = null;
                Texture texture = null;

                // Reserve the required space for vertex data in our locally cached buffer
                int neededVertices = 4 * w * h;
                VertexC1P3T2[] vertexData = new VertexC1P3T2[neededVertices];

                int vertexBaseIndex = 0;

                for (int x = 0; x < w; x++) {
                    for (int y = 0; y < h; y++) {
                        LayerTile tile = layer.Layout[x + y * layer.LayoutWidth];

                        Point2 offset;
                        bool isFlippedX, isFlippedY;
                        if (tile.IsAnimated) {
                            if (tile.TileID < animatedTiles.Count) {
                                offset = animatedTiles[tile.TileID].CurrentTile.MaterialOffset;
                                isFlippedX = (animatedTiles[tile.TileID].CurrentTile.IsFlippedX != tile.IsFlippedX);
                                isFlippedY = (animatedTiles[tile.TileID].CurrentTile.IsFlippedY != tile.IsFlippedY);

                                cachedTexturedBackgroundAnimated = true;
                            } else {
                                continue;
                            }
                        } else {
                            offset = tile.MaterialOffset;
                            isFlippedX = tile.IsFlippedX;
                            isFlippedY = tile.IsFlippedY;
                        }

                        if (material != tile.Material) {
                            // Submit all the vertices as one draw batch
                            device.AddVertices(
                                material,
                                VertexMode.Quads,
                                vertexData,
                                0,
                                vertexBaseIndex);

                            vertexBaseIndex = 0;

                            material = tile.Material.Res;
                            texture = material.MainTexture.Res;
                        }

                        Rect uvRect = new Rect(
                            offset.X * texture.UVRatio.X / texture.ContentWidth,
                            offset.Y * texture.UVRatio.Y / texture.ContentHeight,
                            tileset.TileSize * texture.UVRatio.X / texture.ContentWidth,
                            tileset.TileSize * texture.UVRatio.Y / texture.ContentHeight
                        );

                        if (isFlippedX) {
                            uvRect.X += uvRect.W;
                            uvRect.W *= -1;
                        }
                        if (isFlippedY) {
                            uvRect.Y += uvRect.H;
                            uvRect.H *= -1;
                        }

                        Vector3 renderPos = new Vector3(x * 32, y * 32, 0);
                        float scale = 1.0f;
                        device.PreprocessCoords(ref renderPos, ref scale);

                        renderPos.X = MathF.Round(renderPos.X);
                        renderPos.Y = MathF.Round(renderPos.Y);
                        if (MathF.RoundToInt(device.TargetSize.X) != (MathF.RoundToInt(device.TargetSize.X) / 2) * 2) {
                            renderPos.X += 0.5f;
                        }
                        if (MathF.RoundToInt(device.TargetSize.Y) != (MathF.RoundToInt(device.TargetSize.Y) / 2) * 2) {
                            renderPos.Y += 0.5f;
                        }

                        Vector2 tileXStep = new Vector2(32, 0);
                        Vector2 tileYStep = new Vector2(0, 32);

                        vertexData[vertexBaseIndex + 0].Pos.X = renderPos.X;
                        vertexData[vertexBaseIndex + 0].Pos.Y = renderPos.Y;
                        vertexData[vertexBaseIndex + 0].Pos.Z = renderPos.Z;
                        vertexData[vertexBaseIndex + 0].TexCoord.X = uvRect.X;
                        vertexData[vertexBaseIndex + 0].TexCoord.Y = uvRect.Y;
                        vertexData[vertexBaseIndex + 0].Color = ColorRgba.White;

                        vertexData[vertexBaseIndex + 1].Pos.X = renderPos.X + tileYStep.X;
                        vertexData[vertexBaseIndex + 1].Pos.Y = renderPos.Y + tileYStep.Y;
                        vertexData[vertexBaseIndex + 1].Pos.Z = renderPos.Z;
                        vertexData[vertexBaseIndex + 1].TexCoord.X = uvRect.X;
                        vertexData[vertexBaseIndex + 1].TexCoord.Y = uvRect.Y + uvRect.H;
                        vertexData[vertexBaseIndex + 1].Color = ColorRgba.White;

                        vertexData[vertexBaseIndex + 2].Pos.X = renderPos.X + tileXStep.X + tileYStep.X;
                        vertexData[vertexBaseIndex + 2].Pos.Y = renderPos.Y + tileXStep.Y + tileYStep.Y;
                        vertexData[vertexBaseIndex + 2].Pos.Z = renderPos.Z;
                        vertexData[vertexBaseIndex + 2].TexCoord.X = uvRect.X + uvRect.W;
                        vertexData[vertexBaseIndex + 2].TexCoord.Y = uvRect.Y + uvRect.H;
                        vertexData[vertexBaseIndex + 2].Color = ColorRgba.White;

                        vertexData[vertexBaseIndex + 3].Pos.X = renderPos.X + tileXStep.X;
                        vertexData[vertexBaseIndex + 3].Pos.Y = renderPos.Y + tileXStep.Y;
                        vertexData[vertexBaseIndex + 3].Pos.Z = renderPos.Z;
                        vertexData[vertexBaseIndex + 3].TexCoord.X = uvRect.X + uvRect.W;
                        vertexData[vertexBaseIndex + 3].TexCoord.Y = uvRect.Y;
                        vertexData[vertexBaseIndex + 3].Color = ColorRgba.White;

                        vertexBaseIndex += 4;
                    }
                }

                device.AddVertices(
                    material,
                    VertexMode.Quads,
                    vertexData,
                    0,
                    vertexBaseIndex);

                device.Render();
            }

            cachedTexturedBackground = renderTarget;
        }

        public void RenderTexturedBackground(IDrawDevice device, ref TileMapLayer layer, int cacheIndex, float x, float y)
        {
            if (cachedTexturedBackground == null || cachedTexturedBackgroundAnimated) {
                RecreateTexturedBackground(ref layer);
            }

            // Fit the input material rect to the output size according to rendering step config
            Vector3 renderPos = new Vector3(device.RefCoord.X - device.TargetSize.X / 2, device.RefCoord.Y - device.TargetSize.Y / 2, layer.Depth);
            float scale = 1.0f;
            device.PreprocessCoords(ref renderPos, ref scale);

            // Fit the target rect to actual pixel coordinates to avoid unnecessary filtering offsets
            renderPos.X = MathF.Round(renderPos.X);
            renderPos.Y = MathF.Round(renderPos.Y);
            if (MathF.RoundToInt(device.TargetSize.X) != (MathF.RoundToInt(device.TargetSize.X) / 2) * 2) {
                renderPos.X += 0.5f;
            }
            if (MathF.RoundToInt(device.TargetSize.Y) != (MathF.RoundToInt(device.TargetSize.Y) / 2) * 2) {
                //renderPos.Y += 0.5f;
                // AMD Bugfix?
                renderPos.Y -= 0.001f;
            }

            // Reserve the required space for vertex data in our locally cached buffer
            VertexC1P3T2[] vertexData;

            int neededVertices = 4;
            if (cachedVertices[cacheIndex] == null || cachedVertices[cacheIndex].Length < neededVertices) {
                cachedVertices[cacheIndex] = vertexData = new VertexC1P3T2[neededVertices];
            } else {
                vertexData = cachedVertices[cacheIndex];
            }

            // Render it as world-space fullscreen quad
            vertexData[0].Pos = new Vector3(renderPos.X, renderPos.Y, renderPos.Z);
            vertexData[1].Pos = new Vector3(renderPos.X + device.TargetSize.X, renderPos.Y, renderPos.Z);
            vertexData[2].Pos = new Vector3(renderPos.X + device.TargetSize.X, renderPos.Y + device.TargetSize.Y, renderPos.Z);
            vertexData[3].Pos = new Vector3(renderPos.X, renderPos.Y + device.TargetSize.Y, renderPos.Z);

            vertexData[0].TexCoord = new Vector2(0.0f, 0.0f);
            vertexData[1].TexCoord = new Vector2(1f, 0.0f);
            vertexData[2].TexCoord = new Vector2(1f, 1f);
            vertexData[3].TexCoord = new Vector2(0.0f, 1f);

            vertexData[0].Color = vertexData[1].Color = vertexData[2].Color = vertexData[3].Color = ColorRgba.White;

            // Setup custom pixel shader
            BatchInfo material = new BatchInfo(texturedBackgroundShader, cachedTexturedBackground);
            material.SetValue("horizonColor", new Vector4(layer.BackgroundColor.R / 255f, layer.BackgroundColor.G / 255f, layer.BackgroundColor.B / 255f, layer.BackgroundColor.A / 255f));
            material.SetValue("shift", new Vector2(x, y));

            device.AddVertices(material, VertexMode.Quads, vertexData);
        }
    }
}