#if !SERVER

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

            Texture targetTexture;
            if (cachedTexturedBackground.IsAvailable) {
                targetTexture = cachedTexturedBackground.Res;
            } else {
                targetTexture = new Texture(w * 32, h * 32, TextureSizeMode.NonPowerOfTwo, TextureMagFilter.Linear, TextureMinFilter.Linear, TextureWrapMode.Repeat, TextureWrapMode.Repeat);

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
                device.Projection = ProjectionMode.Screen;

                using (RenderTarget target = new RenderTarget(AAQuality.Off, false, targetTexture)) {
                    device.Target = target;
                    device.TargetSize = new Vector2(w * 32, h * 32);
                    device.ViewportRect = new Rect(device.TargetSize);

                    device.PrepareForDrawcalls();

                    Material material = null;
                    Texture texture = null;

                    // Reserve the required space for vertex data in our locally cached buffer
                    int neededVertices = 4 * w * h;
                    if (cachedVertices == null || cachedVertices.Length < neededVertices) {
                        cachedVertices = new VertexC1P3T2[neededVertices];
                    }

                    int vertexIndex = 0;

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
                                    cachedVertices,
                                    0,
                                    vertexIndex);

                                vertexIndex = 0;

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

                            cachedVertices[vertexIndex].Pos.X = renderPos.X;
                            cachedVertices[vertexIndex].Pos.Y = renderPos.Y;
                            cachedVertices[vertexIndex].Pos.Z = renderPos.Z;
                            cachedVertices[vertexIndex].TexCoord.X = uvRect.X;
                            cachedVertices[vertexIndex].TexCoord.Y = uvRect.Y;
                            cachedVertices[vertexIndex].Color = ColorRgba.White;

                            cachedVertices[vertexIndex + 1].Pos.X = renderPos.X + tileYStep.X;
                            cachedVertices[vertexIndex + 1].Pos.Y = renderPos.Y + tileYStep.Y;
                            cachedVertices[vertexIndex + 1].Pos.Z = renderPos.Z;
                            cachedVertices[vertexIndex + 1].TexCoord.X = uvRect.X;
                            cachedVertices[vertexIndex + 1].TexCoord.Y = uvRect.Y + uvRect.H;
                            cachedVertices[vertexIndex + 1].Color = ColorRgba.White;

                            cachedVertices[vertexIndex + 2].Pos.X = renderPos.X + tileXStep.X + tileYStep.X;
                            cachedVertices[vertexIndex + 2].Pos.Y = renderPos.Y + tileXStep.Y + tileYStep.Y;
                            cachedVertices[vertexIndex + 2].Pos.Z = renderPos.Z;
                            cachedVertices[vertexIndex + 2].TexCoord.X = uvRect.X + uvRect.W;
                            cachedVertices[vertexIndex + 2].TexCoord.Y = uvRect.Y + uvRect.H;
                            cachedVertices[vertexIndex + 2].Color = ColorRgba.White;

                            cachedVertices[vertexIndex + 3].Pos.X = renderPos.X + tileXStep.X;
                            cachedVertices[vertexIndex + 3].Pos.Y = renderPos.Y + tileXStep.Y;
                            cachedVertices[vertexIndex + 3].Pos.Z = renderPos.Z;
                            cachedVertices[vertexIndex + 3].TexCoord.X = uvRect.X + uvRect.W;
                            cachedVertices[vertexIndex + 3].TexCoord.Y = uvRect.Y;
                            cachedVertices[vertexIndex + 3].Color = ColorRgba.White;

                            vertexIndex += 4;
                        }
                    }

                    device.AddVertices(material, VertexMode.Quads, cachedVertices, 0, vertexIndex);

                    device.Render();
                }
            }

            cachedTexturedBackground = targetTexture;
        }

        public void RenderTexturedBackground(IDrawDevice device, ref TileMapLayer layer, int cacheIndex, float x, float y)
        {
            if (!cachedTexturedBackground.IsAvailable || cachedTexturedBackgroundAnimated) {
                RecreateTexturedBackground(ref layer);
            }

            // Fit the input material rect to the output size according to rendering step config
            Vector3 renderPos = new Vector3(device.ViewerPos.X - device.TargetSize.X / 2, device.ViewerPos.Y - device.TargetSize.Y / 2, layer.Depth);

            // Fit the target rect to actual pixel coordinates to avoid unnecessary filtering offsets
            renderPos.X = MathF.Round(renderPos.X);
            renderPos.Y = MathF.Round(renderPos.Y);
            if (MathF.RoundToInt(device.TargetSize.X) != (MathF.RoundToInt(device.TargetSize.X) / 2) * 2) {
                renderPos.X += 0.5f;
            }
            if (MathF.RoundToInt(device.TargetSize.Y) != (MathF.RoundToInt(device.TargetSize.Y) / 2) * 2) {
                // AMD Bugfix?
                renderPos.Y -= 0.004f;
            }

            // Reserve the required space for vertex data in our locally cached buffer
            int neededVertices = 4;
            if (cachedVertices == null || cachedVertices.Length < neededVertices) {
                cachedVertices = new VertexC1P3T2[neededVertices];
            }

            // Render it as world-space fullscreen quad
            cachedVertices[0].Pos = new Vector3(renderPos.X, renderPos.Y, renderPos.Z);
            cachedVertices[1].Pos = new Vector3(renderPos.X + device.TargetSize.X, renderPos.Y, renderPos.Z);
            cachedVertices[2].Pos = new Vector3(renderPos.X + device.TargetSize.X, renderPos.Y + device.TargetSize.Y, renderPos.Z);
            cachedVertices[3].Pos = new Vector3(renderPos.X, renderPos.Y + device.TargetSize.Y, renderPos.Z);

            cachedVertices[0].TexCoord = new Vector2(0f, 0f);
            cachedVertices[1].TexCoord = new Vector2(1f, 0f);
            cachedVertices[2].TexCoord = new Vector2(1f, 1f);
            cachedVertices[3].TexCoord = new Vector2(0f, 1f);

            cachedVertices[0].Color = cachedVertices[1].Color = cachedVertices[2].Color = cachedVertices[3].Color = ColorRgba.White;

            // Setup custom pixel shader
            BatchInfo material = device.RentMaterial();
            material.Technique = texturedBackgroundShader;
            material.MainTexture = cachedTexturedBackground;
            material.SetValue("horizonColor", layer.BackgroundColor);
            material.SetValue("shift", new Vector2(x, y));
            material.SetValue("parallaxStarsEnabled", layer.ParallaxStarsEnabled ? 1f : 0f);

            device.AddVertices(material, VertexMode.Quads, cachedVertices, 0, 4);
        }
    }
}

#endif