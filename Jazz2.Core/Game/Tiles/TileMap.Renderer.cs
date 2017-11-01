using System;
using System.Runtime.CompilerServices;
using Duality;
using Duality.Drawing;
using Duality.Resources;
using Jazz2.Game.Structs;

namespace Jazz2.Game.Tiles
{
    partial class TileMap : ICmpRenderer
    {
        private VertexC1P3T2[][] cachedVertices;

        void ICmpRenderer.GetCullingInfo(out CullingInfo info)
        {
            info.Position = new Vector3(0f, 0f, 500f);
            info.Radius = float.MaxValue;
            info.Visibility = VisibilityFlag.Group0;
        }

        void ICmpRenderer.Draw(IDrawDevice device)
        {
            if (tileset == null) {
                return;
            }

            if (cachedVertices == null || cachedVertices.Length != this.layers.Count) {
                cachedVertices = new VertexC1P3T2[this.layers.Count][];
            }

            TileMapLayer[] layersRaw = layers.Data;
            for (int i = this.layers.Count - 1; i >= 0; i--) {
                DrawLayer(device, ref layersRaw[i], i);
            }

            DrawDebris(device);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float TranslateCoordinate(float coordinate, float speed, float offset, bool isY, float viewHeight, float viewWidth)
        {
            // Coordinate: the "vanilla" coordinate of the tile on the layer if the layer was fixed to the sprite layer with same
            // speed and no other options. Think of its position in JCS.
            // Speed: the set layer speed; 1 for anything that moves the same speed as the sprite layer (where the objects live),
            // less than 1 for backgrounds that move slower, more than 1 for foregrounds that move faster
            // Offset: any difference to starting coordinates caused by an inherent automatic speed a layer has

            // Literal 70 is the same as in DrawLayer, it's the offscreen offset of the first tile to draw.
            // Don't touch unless absolutely necessary.
            return (coordinate * speed + offset + (70 + (isY ? (viewHeight - 200) : (viewWidth - 320)) / 2) * (speed - 1));
        }

        private void DrawLayer(IDrawDevice device, ref TileMapLayer layer, int cacheIndex)
        {
            if (!layer.Visible) {
                return;
            }

            Vector2 viewSize = device.TargetSize;
            Vector3 viewCenter = device.RefCoord;

            Point2 tileCount = new Point2(layer.LayoutWidth, layer.Layout.Length / layer.LayoutWidth);
            Vector2 tileSize = new Vector2(tileset.TileSize, tileset.TileSize);

            // Update offsets for moving layers
            if (MathF.Abs(layer.AutoSpeedX) > 0) {
                layer.OffsetX += layer.AutoSpeedX * Time.TimeMult;
                if (layer.RepeatX) {
                    if (layer.AutoSpeedX > 0) {
                        while (layer.OffsetX > (tileCount.X * 32)) {
                            layer.OffsetX -= (tileCount.X * 32);
                        }
                    } else {
                        while (layer.OffsetX < 0) {
                            layer.OffsetX += (tileCount.X * 32);
                        }
                    }
                }
            }
            if (MathF.Abs(layer.AutoSpeedY) > 0) {
                layer.OffsetY += layer.AutoSpeedY * Time.TimeMult;
                if (layer.RepeatY) {
                    if (layer.AutoSpeedY > 0) {
                        while (layer.OffsetY > (tileCount.Y * 32)) {
                            layer.OffsetY -= (tileCount.Y * 32);
                        }
                    } else {
                        while (layer.OffsetY < 0) {
                            layer.OffsetY += (tileCount.Y * 32);
                        }
                    }
                }
            }

            // Get current layer offsets and speeds
            float loX = layer.OffsetX;
            float loY = layer.OffsetY - (layer.UseInherentOffset ? (viewSize.Y - 200) / 2 : 0);

            // Find out coordinates for a tile from outside the boundaries from topleft corner of the screen 
            float x1 = viewCenter.X - 70 - (viewSize.X * 0.5f);
            float y1 = viewCenter.Y - 70 - (viewSize.Y * 0.5f);

            if (layer.BackgroundStyle != BackgroundStyle.Plain && tileCount.Y == 8 && tileCount.X == 8) {
                const float PerspectiveSpeedX = 0.4f;
                const float PerspectiveSpeedY = 0.16f;
                RenderTexturedBackground(device, ref layer, cacheIndex,
                    (x1 * PerspectiveSpeedX + loX),
                    (y1 * PerspectiveSpeedY + loY));
            } else {
                // Figure out the floating point offset from the calculated coordinates and the actual tile
                // corner coordinates
                float xt = TranslateCoordinate(x1, layer.SpeedX, loX, false, viewSize.Y, viewSize.X);
                float yt = TranslateCoordinate(y1, layer.SpeedY, loY, true, viewSize.Y, viewSize.X);

                float remX = xt % 32f;
                float remY = yt % 32f;

                // Calculate the index (on the layer map) of the first tile that needs to be drawn to the
                // position determined earlier
                int tileX, tileY, tileAbsX, tileAbsY;

                // Get the actual tile coords on the layer layout
                if (xt > 0) {
                    tileAbsX = (int)Math.Floor(xt / 32f);
                    tileX = tileAbsX % tileCount.X;
                } else {
                    tileAbsX = (int)Math.Ceiling(xt / 32f);
                    tileX = tileAbsX % tileCount.X;
                    while (tileX < 0) {
                        tileX += tileCount.X;
                    }
                }

                if (yt > 0) {
                    tileAbsY = (int)Math.Floor(yt / 32f);
                    tileY = tileAbsY % tileCount.Y;
                } else {
                    tileAbsY = (int)Math.Ceiling(yt / 32f);
                    tileY = tileAbsY % tileCount.Y;
                    while (tileY < 0) {
                        tileY += tileCount.Y;
                    }
                }

                // update x1 and y1 with the remainder so that we start at the tile boundary
                // minus 1 because indices are updated in the beginning of the loops
                x1 -= remX - 32f;
                y1 -= remY - 32f;

                // Save the tile Y at the left border so that we can roll back to it at the start of
                // every row iteration
                int tileYs = tileY;

                // Calculate the last coordinates we want to draw to
                float x3 = x1 + 100 + viewSize.X;
                float y3 = y1 + 100 + viewSize.Y;

                Material material = tileset.Material.Res;
                Texture texture = material.MainTexture.Res;
                ColorRgba mainColor = ColorRgba.White;

                // Reserve the required space for vertex data in our locally cached buffer
                VertexC1P3T2[] vertexData;

                int neededVertices = (int)((((x3 - x1) / 32) + 1) * (((y3 - y1) / 32) + 1) * 4);
                if (cachedVertices[cacheIndex] == null || cachedVertices[cacheIndex].Length < neededVertices) {
                    cachedVertices[cacheIndex] = vertexData = new VertexC1P3T2[neededVertices];
                } else {
                    vertexData = cachedVertices[cacheIndex];
                }

                int vertexBaseIndex = 0;

                int tile_xo = -1;
                for (float x2 = x1; x2 < x3; x2 += 32) {
                    tileX = (tileX + 1) % tileCount.X;
                    tile_xo++;
                    if (!layer.RepeatX) {
                        // If the current tile isn't in the first iteration of the layer horizontally, don't draw this column
                        if (tileAbsX + tile_xo + 1 < 0 || tileAbsX + tile_xo + 1 >= tileCount.X) {
                            continue;
                        }
                    }
                    tileY = tileYs;
                    int tile_yo = -1;
                    for (float y2 = y1; y2 < y3; y2 += 32) {
                        tileY = (tileY + 1) % tileCount.Y;
                        tile_yo++;

                        LayerTile tile = layer.Layout[tileX + tileY * layer.LayoutWidth];

                        if (!layer.RepeatY) {
                            // If the current tile isn't in the first iteration of the layer vertically, don't draw it
                            if (tileAbsY + tile_yo + 1 < 0 || tileAbsY + tile_yo + 1 >= tileCount.Y) {
                                continue;
                            }
                        }

                        Point2 offset;
                        bool isFlippedX, isFlippedY;
                        if (tile.IsAnimated) {
                            if (tile.TileID < animatedTiles.Count) {
                                offset = animatedTiles[tile.TileID].CurrentTile.MaterialOffset;
                                isFlippedX = (animatedTiles[tile.TileID].CurrentTile.IsFlippedX != tile.IsFlippedX);
                                isFlippedY = (animatedTiles[tile.TileID].CurrentTile.IsFlippedY != tile.IsFlippedY);

                                //mainColor.A = tile.MaterialAlpha;
                                mainColor.A = animatedTiles[tile.TileID].CurrentTile.MaterialAlpha;
                            } else {
                                continue;
                            }
                        } else {
                            offset = tile.MaterialOffset;
                            isFlippedX = tile.IsFlippedX;
                            isFlippedY = tile.IsFlippedY;

                            mainColor.A = tile.MaterialAlpha;
                        }

                        Rect uvRect = new Rect(
                            offset.X * texture.UVRatio.X / texture.ContentWidth,
                            offset.Y * texture.UVRatio.Y / texture.ContentHeight,
                            tileset.TileSize * texture.UVRatio.X / texture.ContentWidth,
                            tileset.TileSize * texture.UVRatio.Y / texture.ContentHeight
                        );

                        // ToDo: Flip normal map somehow
                        if (isFlippedX) {
                            uvRect.X += uvRect.W;
                            uvRect.W *= -1;
                        }
                        if (isFlippedY) {
                            uvRect.Y += uvRect.H;
                            uvRect.H *= -1;
                        }

                        Vector3 renderPos = new Vector3(x2, y2, layer.Depth);
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

                        vertexData[vertexBaseIndex + 0].Pos.X = renderPos.X;
                        vertexData[vertexBaseIndex + 0].Pos.Y = renderPos.Y;
                        vertexData[vertexBaseIndex + 0].Pos.Z = renderPos.Z;
                        vertexData[vertexBaseIndex + 0].TexCoord.X = uvRect.X;
                        vertexData[vertexBaseIndex + 0].TexCoord.Y = uvRect.Y;
                        vertexData[vertexBaseIndex + 0].Color = mainColor;

                        vertexData[vertexBaseIndex + 1].Pos.X = renderPos.X;
                        vertexData[vertexBaseIndex + 1].Pos.Y = renderPos.Y + tileSize.Y;
                        vertexData[vertexBaseIndex + 1].Pos.Z = renderPos.Z;
                        vertexData[vertexBaseIndex + 1].TexCoord.X = uvRect.X;
                        vertexData[vertexBaseIndex + 1].TexCoord.Y = uvRect.Y + uvRect.H;
                        vertexData[vertexBaseIndex + 1].Color = mainColor;

                        vertexData[vertexBaseIndex + 2].Pos.X = renderPos.X + tileSize.X;
                        vertexData[vertexBaseIndex + 2].Pos.Y = renderPos.Y + tileSize.Y;
                        vertexData[vertexBaseIndex + 2].Pos.Z = renderPos.Z;
                        vertexData[vertexBaseIndex + 2].TexCoord.X = uvRect.X + uvRect.W;
                        vertexData[vertexBaseIndex + 2].TexCoord.Y = uvRect.Y + uvRect.H;
                        vertexData[vertexBaseIndex + 2].Color = mainColor;

                        vertexData[vertexBaseIndex + 3].Pos.X = renderPos.X + tileSize.X;
                        vertexData[vertexBaseIndex + 3].Pos.Y = renderPos.Y;
                        vertexData[vertexBaseIndex + 3].Pos.Z = renderPos.Z;
                        vertexData[vertexBaseIndex + 3].TexCoord.X = uvRect.X + uvRect.W;
                        vertexData[vertexBaseIndex + 3].TexCoord.Y = uvRect.Y;
                        vertexData[vertexBaseIndex + 3].Color = mainColor;

                        vertexBaseIndex += 4;
                    }
                }

                // Submit all the vertices as one draw batch
                device.AddVertices(
                    material,
                    VertexMode.Quads,
                    vertexData,
                    0,
                    vertexBaseIndex);
            }
        }
    }
}