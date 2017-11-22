using System;
using System.Drawing;
using System.Windows.Forms;
using Duality;
using Duality.Drawing;
using Duality.Resources;
using Jazz2.Game.Tiles;

namespace Editor.UI.Controls
{
    public class TileSetView : RenderControl
    {
        private VScrollBar scrollBar;

        private TileMap tilemap;

        public TileSetView()
        {


            scrollBar = new VScrollBar();
            scrollBar.Dock = DockStyle.Right;
            scrollBar.ValueChanged += ScrollBar_ValueChanged;
            Controls.Add(scrollBar);

        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            Scene.Entered += Scene_Changed;
            Scene.Leaving += Scene_Changed;

            Scene_Changed(this, EventArgs.Empty);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);

            Scene.Entered -= Scene_Changed;
            Scene.Leaving -= Scene_Changed;
        }

        private void Scene_Changed(object sender, System.EventArgs e)
        {
            EditorLevelHandler levelHandler = Scene.Current as EditorLevelHandler;
            if (levelHandler != null) {
                tilemap = levelHandler.TileMap;

                int tilesY = tilemap.Tileset.TileCount / 10;

                scrollBar.Minimum = 0;
                scrollBar.Maximum = tilesY * tilemap.Tileset.TileSize/* - ClientSize.Height*/;

                scrollBar.SmallChange = 8;
                scrollBar.LargeChange = ClientSize.Height / 2;
            } else {
                tilemap = null;
            }
        }

        private void ScrollBar_ValueChanged(object sender, EventArgs e)
        {
            InvalidateView();
        }

        protected override void OnRender(Canvas canvas)
        {
            Size size = ClientSize;
            size.Width -= scrollBar.ClientSize.Width;

            canvas.State.ColorTint = ColorRgba.White;
            canvas.FillRect(0, 0, size.Width, size.Height);

            if (tilemap == null) {
                return;
            }

            IDrawDevice device = canvas.DrawDevice;

            TileSet tileset = tilemap.Tileset;
            Texture texture = tilemap.Tileset.Material.Res.MainTexture.Res;

            int scrollBarOffset = scrollBar.Value;

            ColorRgba mainColor = ColorRgba.White;
            Point2 tileSize = new Point2(tilemap.Tileset.TileSize, tilemap.Tileset.TileSize);

            VertexC1P3T2[] vertexData = new VertexC1P3T2[4];

            int tileIndex = 0;
            for (int y = 0; tileIndex < tileset.TileCount; y += tileSize.Y) {
                for (int x = 0; x < 10 * tileSize.X; x += tileSize.X) {

                    int tx = (tileIndex % tileset.TilesPerRow) * tileSize.X;
                    int ty = (tileIndex / tileset.TilesPerRow) * tileSize.Y;

                    Vector3 renderPos = new Vector3(x, y - scrollBarOffset, 0);

                    Rect uvRect = new Rect(
                        tx * texture.UVRatio.X / texture.ContentWidth,
                        ty * texture.UVRatio.Y / texture.ContentHeight,
                        tileset.TileSize * texture.UVRatio.X / texture.ContentWidth,
                        tileset.TileSize * texture.UVRatio.Y / texture.ContentHeight
                    );

                    vertexData[0].Pos.X = renderPos.X;
                    vertexData[0].Pos.Y = renderPos.Y;
                    vertexData[0].Pos.Z = renderPos.Z;
                    vertexData[0].TexCoord.X = uvRect.X;
                    vertexData[0].TexCoord.Y = uvRect.Y;
                    vertexData[0].Color = mainColor;

                    vertexData[1].Pos.X = renderPos.X;
                    vertexData[1].Pos.Y = renderPos.Y + tileSize.Y;
                    vertexData[1].Pos.Z = renderPos.Z;
                    vertexData[1].TexCoord.X = uvRect.X;
                    vertexData[1].TexCoord.Y = uvRect.Y + uvRect.H;
                    vertexData[1].Color = mainColor;

                    vertexData[2].Pos.X = renderPos.X + tileSize.X;
                    vertexData[2].Pos.Y = renderPos.Y + tileSize.Y;
                    vertexData[2].Pos.Z = renderPos.Z;
                    vertexData[2].TexCoord.X = uvRect.X + uvRect.W;
                    vertexData[2].TexCoord.Y = uvRect.Y + uvRect.H;
                    vertexData[2].Color = mainColor;

                    vertexData[3].Pos.X = renderPos.X + tileSize.X;
                    vertexData[3].Pos.Y = renderPos.Y;
                    vertexData[3].Pos.Z = renderPos.Z;
                    vertexData[3].TexCoord.X = uvRect.X + uvRect.W;
                    vertexData[3].TexCoord.Y = uvRect.Y;
                    vertexData[3].Color = mainColor;

                    device.AddVertices(tilemap.Tileset.Material, VertexMode.Quads, vertexData);

                    tileIndex++;
                }
            }
        }
    }
}