using System;
using System.Drawing;
using Duality.Drawing;
using Duality.Resources;
using Jazz2.Game.Tiles;

namespace Editor.UI.Controls
{
    public class TileMapLayersListView : RenderControl
    {
        private TileMap tilemap;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            Scene.Entered += Scene_Changed;
            Scene.Leaving += Scene_Changed;
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
            } else {
                tilemap = null;
            }
        }

        protected override void OnRender(Canvas canvas)
        {
            Size size = ClientSize;

            canvas.State.ColorTint = ColorRgba.White;
            canvas.FillRect(0, 0, size.Width, size.Height);

            canvas.State.ColorTint = ColorRgba.LightGrey;
            canvas.DrawRect(0, 0, size.Width, size.Height);

            //canvas.State.ColorTint = ColorRgba.Black;
            //canvas.FillRect(10, 10, 100, 100);

            VertexC1P3T2[] vertices = null;
            {
                int vertexCount = FontRasterizer.Window9.EmitTextVertices("ToDo: TileMap Layers", ref vertices, 10, 10, ColorRgba.White);
                canvas.DrawDevice.AddVertices(FontRasterizer.Window9.Material, VertexMode.Quads, vertices, 0, vertexCount);
            }

        }
    }
}