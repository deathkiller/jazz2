using System;
using System.Drawing;
using System.Windows.Forms;
using Duality;
using Duality.Drawing;
using Duality.Resources;
using Jazz2.Game;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;

namespace Editor.UI.CamView.States
{
    /// <summary>
    /// Provides a full preview of the game within the editor. 
    /// This state renders the games actual audiovisual output and reroutes user input to the game.
    /// </summary>
    public class TileMapEditorState : CamViewState
    {
        /// <summary>
        /// Describes rendering size preset roles that may have special behaviour.
        /// </summary>
        private enum SpecialRenderSize
        {
            /// <summary>
            /// A fixed size is used, regardless of game settings or window sizes.
            /// </summary>
            Fixed,

            /// <summary>
            /// Matches the <see cref="CamView"/> client size.
            /// </summary>
            CamView,

            /// <summary>
            /// Matches the target size of the game, based on forced rendering size
            /// settings and default screen resolution.
            /// </summary>
            GameTarget
        }

        [Flags]
        public enum TileMapDragMode
        {
            None = 0,

            West = 1 << 0,
            East = 1 << 1,
            North = 1 << 2,
            South = 1 << 3
        }

        private const int TileSize = 32;

        private Point2 targetRenderSize = Point2.Zero;
        private SpecialRenderSize targetRenderSizeMode = SpecialRenderSize.CamView;
        private bool isUpdatingUI;

        private TileMap tilemap;
        private Point activeTilePos;
        private Rect activeTileRect;

        private bool tileMapDragActive;
        private TileMapDragMode tileMapDragMode;

        /// <inheritdoc />
        public override string StateName
        {
            get { return "TileMapEditor"; }
        }

        /// <inheritdoc />
        public override Rect RenderedViewport
        {
            get { return this.LocalGameWindowRect; }
        }

        /// <inheritdoc />
        public override Point2 RenderedImageSize
        {
            get { return this.TargetRenderSize; }
        }

        /// <summary>
        /// [GET] The target rendering size that is preferred by the game.
        /// Depends on default window size and forced resolution settings.
        /// </summary>
        private Point2 GameTargetSize
        {
            get
            {
                return new Point2(
                    this.RenderableControl.ClientSize.Width,
                    this.RenderableControl.ClientSize.Height);
            }
        }

        /// <summary>
        /// [GET] The target rendering size that is preferred by the <see cref="CamView"/>
        /// client area.
        /// </summary>
        private Point2 CamViewTargetSize
        {
            get
            {
                return new Point2(
                    this.RenderableControl.ClientSize.Width,
                    this.RenderableControl.ClientSize.Height);
            }
        }

        /// <summary>
        /// [GET / SET] The rendering size that will be used for displaying the
        /// game in this <see cref="CamViewState"/>.
        /// </summary>
        private Point2 TargetRenderSize
        {
            get { return this.targetRenderSize; }
            set
            {
                if (this.targetRenderSize != value) {
                    this.targetRenderSize = value;
                    this.Invalidate();
                }
            }
        }

        /// <summary>
        /// [GET] Whether the currently used <see cref="TargetRenderSize"/> fits
        /// completely inside the available client area without having to downscale.
        /// </summary>
        private bool TargetSizeFitsClientArea
        {
            get
            {
                return
                    this.targetRenderSize.X <= this.RenderableControl.ClientSize.Width &&
                    this.targetRenderSize.Y <= this.RenderableControl.ClientSize.Height;
            }
        }

        /// <summary>
        /// [GET] The rect inside the local <see cref="CamView"/> client area that
        /// will be occupied by the game rendering. Pixels outside this rect will
        /// not be rendered to by the game.
        /// </summary>
        private Rect LocalGameWindowRect
        {
            get
            {
                TargetResize resizeMode = this.TargetSizeFitsClientArea ? TargetResize.None : TargetResize.Fit;

                Vector2 clientSize = new Vector2(this.ClientSize.Width, this.ClientSize.Height);
                Vector2 localWindowSize = resizeMode.Apply(this.TargetRenderSize, clientSize);

                return Rect.Align(
                    Alignment.Center,
                    clientSize.X * 0.5f,
                    clientSize.Y * 0.5f,
                    localWindowSize.X,
                    localWindowSize.Y);
            }
        }


        public TileMapEditorState()
        {
            this.CameraActionAllowed = false;
            this.EngineUserInput = true;

            CameraActionAllowed = true;
        }

        protected override void OnSceneChanged()
        {
            base.OnSceneChanged();

            EditorLevelHandler levelHandler = Scene.Current as EditorLevelHandler;
            if (levelHandler != null) {
                tilemap = levelHandler.TileMap;
            } else {
                tilemap = null;
            }
        }

        /// <summary>
        /// Applies the dynamic rendering size that is defined by the currently
        /// used <see cref="TargetRenderSizeMode"/>. Does nothing if that mode is
        /// <see cref="SpecialRenderSize.Fixed"/>.
        /// </summary>
        private void ApplyTargetRenderSizeMode()
        {
            if (this.targetRenderSizeMode == SpecialRenderSize.CamView)
                this.TargetRenderSize = this.CamViewTargetSize;
            else if (this.targetRenderSizeMode == SpecialRenderSize.GameTarget)
                this.TargetRenderSize = this.GameTargetSize;
        }

        /// <inheritdoc />
        protected internal override void OnEnterState()
        {
            base.OnEnterState();

            this.CameraObj.Active = false;

            this.ApplyTargetRenderSizeMode();


            CameraObj.Transform.Pos = new Vector3(
                RenderedImageSize.X / 2 - TileSize * 2,
                RenderedImageSize.Y / 2 - TileSize * 2,
                0);
        }

        /// <inheritdoc />
        protected internal override void OnLeaveState()
        {
            base.OnLeaveState();

            this.CameraObj.Active = true;
        }

        /// <inheritdoc />
        protected override void OnResize()
        {
            base.OnResize();

            // Update target size when fitting to cam view size
            if (this.targetRenderSizeMode == SpecialRenderSize.CamView)
                this.TargetRenderSize = this.CamViewTargetSize;
        }

        protected override void OnCollectStateWorldOverlayDrawcalls(Canvas canvas)
        {
            base.OnCollectStateWorldOverlayDrawcalls(canvas);

            if (tilemap != null) {
                Rect bounds = new Rect(-1, -1, tilemap.Size.X * 32 + 2, tilemap.Size.Y * 32 + 2);

                canvas.State.SetMaterial(DrawTechnique.Alpha);

                if (tileMapDragActive) {
                    canvas.State.ColorTint = ColorRgba.Black.WithAlpha(0.2f);
                    canvas.DrawRect(bounds.X - 3, bounds.Y - 3, bounds.W + 6, bounds.H + 6);

                    canvas.State.ColorTint = ColorRgba.Black.WithAlpha(0.3f);
                    canvas.DrawRect(bounds.X - 2, bounds.Y - 2, bounds.W + 4, bounds.H + 4);
                    canvas.DrawRect(bounds.X + 2, bounds.Y + 2, bounds.W - 4, bounds.H - 4);

                    canvas.State.ColorTint = ColorRgba.Black.WithAlpha(0.9f);
                    canvas.DrawRect(bounds.X - 1, bounds.Y - 1, bounds.W + 2, bounds.H + 2);
                    canvas.DrawRect(bounds.X + 1, bounds.Y + 1, bounds.W - 2, bounds.H - 2);

                    canvas.State.ColorTint = new ColorRgba(1f, 0.8f, 0.4f);
                    canvas.DrawRect(bounds.X, bounds.Y, bounds.W, bounds.H);
                } else if (tileMapDragMode != TileMapDragMode.None) {
                    canvas.State.ColorTint = ColorRgba.Black.WithAlpha(0.2f);
                    canvas.DrawRect(bounds.X - 3, bounds.Y - 3, bounds.W + 6, bounds.H + 6);

                    canvas.State.ColorTint = ColorRgba.Black.WithAlpha(0.3f);
                    canvas.DrawRect(bounds.X - 2, bounds.Y - 2, bounds.W + 4, bounds.H + 4);
                    canvas.DrawRect(bounds.X + 2, bounds.Y + 2, bounds.W - 4, bounds.H - 4);

                    canvas.State.ColorTint = ColorRgba.Black.WithAlpha(0.9f);
                    canvas.DrawRect(bounds.X - 1, bounds.Y - 1, bounds.W + 2, bounds.H + 2);
                    canvas.DrawRect(bounds.X + 1, bounds.Y + 1, bounds.W - 2, bounds.H - 2);

                    canvas.State.ColorTint = ColorRgba.White;
                    canvas.DrawRect(bounds.X, bounds.Y, bounds.W, bounds.H);
                } else {
                    canvas.State.ColorTint = ColorRgba.Black.WithAlpha(0.2f);
                    canvas.DrawRect(bounds.X - 2, bounds.Y - 2, bounds.W + 4, bounds.H + 4);

                    canvas.State.ColorTint = ColorRgba.Black.WithAlpha(0.4f);
                    canvas.DrawRect(bounds.X - 1, bounds.Y - 1, bounds.W + 2, bounds.H + 2);

                    canvas.State.ColorTint = ColorRgba.White.WithAlpha(0.7f);
                    canvas.DrawRect(bounds.X, bounds.Y, bounds.W, bounds.H);
                }

                if (bounds.Contains(activeTileRect)) {
                    canvas.State.ColorTint = ColorRgba.White;
                    canvas.DrawRect(activeTileRect.X, activeTileRect.Y, activeTileRect.W, activeTileRect.H);
                }
            }
        }

        protected override void OnCollectStateOverlayDrawcalls(Canvas canvas)
        {
            base.OnCollectStateOverlayDrawcalls(canvas);

            if (CamViewTargetSize.X > LevelRenderSetup.TargetSize.X ||
                CamViewTargetSize.Y > LevelRenderSetup.TargetSize.Y) {

                float x1 = (CamViewTargetSize.X - LevelRenderSetup.TargetSize.X) / 2;
                float y1 = (CamViewTargetSize.Y - LevelRenderSetup.TargetSize.Y) / 2;
                float x2 = x1 + LevelRenderSetup.TargetSize.X;
                float y2 = y1 + LevelRenderSetup.TargetSize.Y;

                canvas.State.SetMaterial(DrawTechnique.Alpha);
                canvas.State.ColorTint = new ColorRgba(0.4f, 1f, 0.4f);

                canvas.DrawDashLine(x1, y1, x2, y1, DashPattern.Dash);
                canvas.DrawDashLine(x1, y1, x1, y2, DashPattern.Dash);
                canvas.DrawDashLine(x2, y1, x2, y2, DashPattern.Dash);
                canvas.DrawDashLine(x1, y2, x2, y2, DashPattern.Dash);
            }

            VertexC1P3T2[] vertices = null;
            {
                int vertexCount = FontRasterizer.Native9.EmitTextVertices("Pre-alpha version", ref vertices, 10, 10, ColorRgba.White);
                canvas.DrawDevice.AddVertices(FontRasterizer.Native9.Material, VertexMode.Quads, vertices, 0, vertexCount);
            }

            {
                int vertexCount = FontRasterizer.Native9.EmitTextVertices("Tile: " + activeTilePos.X + "; " + activeTilePos.Y, ref vertices, 10, 28, ColorRgba.White);
                canvas.DrawDevice.AddVertices(FontRasterizer.Native9.Material, VertexMode.Quads, vertices, 0, vertexCount);
            }

            if (activeTilePos.X >= 0 && activeTilePos.Y >= 0) {
                ref TileMapLayer layer = ref tilemap.Layers.Data[tilemap.SpriteLayerIndex];

                int index = activeTilePos.X + activeTilePos.Y * layer.LayoutWidth;
                if (index < layer.Layout.Length) {
                    ref LayerTile tile = ref layer.Layout[index];

                    int vertexCount = FontRasterizer.Native9.EmitTextVertices("ID: " + tile.TileID + "; Anim: " + tile.IsAnimated + "; FlipX: " + tile.IsFlippedX + "; FlipY: " + tile.IsFlippedY + "; OneWay:" + tile.IsOneWay + "; " + tile.MaterialOffset + "; " + tile.Material, ref vertices, 10, 56, ColorRgba.White);
                    canvas.DrawDevice.AddVertices(FontRasterizer.Native9.Material, VertexMode.Quads, vertices, 0, vertexCount);
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            Vector3 cursor = GetSpaceCoord(new Vector2(e.X, e.Y));

            if (tileMapDragActive) {
                Invalidate();
            } else if (tilemap != null) {
                const int padding = 4;

                activeTilePos.X = (int)MathF.Floor(cursor.X / TileSize);
                activeTilePos.Y = (int)MathF.Floor(cursor.Y / TileSize);

                activeTileRect.X = ((int)cursor.X & ~(TileSize - 1)) - 1;
                activeTileRect.Y = ((int)cursor.Y & ~(TileSize - 1)) - 1;
                activeTileRect.W = TileSize + 2;
                activeTileRect.H = TileSize + 2;

                int areaX = tilemap.Size.X * TileSize;
                int areaY = tilemap.Size.Y * TileSize;

                TileMapDragMode newTileMapDragMode = TileMapDragMode.None;
                if (cursor.Y >= -1 - padding && cursor.Y <= areaY + padding) {
                    if (MathF.Abs(cursor.X - (-1)) < padding) {
                        newTileMapDragMode |= TileMapDragMode.West;
                    } else if (MathF.Abs(cursor.X - areaX) < padding) {
                        newTileMapDragMode |= TileMapDragMode.East;
                    }
                }
                if (cursor.X >= -1 - padding && cursor.X <= areaX + padding) {
                    if (MathF.Abs(cursor.Y - (-1)) < padding) {
                        newTileMapDragMode |= TileMapDragMode.North;
                    } else if (MathF.Abs(cursor.Y - areaY) < padding) {
                        newTileMapDragMode |= TileMapDragMode.South;
                    }
                }

                if (tileMapDragMode != newTileMapDragMode) {
                    tileMapDragMode = newTileMapDragMode;

                    if (tileMapDragMode == TileMapDragMode.West || tileMapDragMode == TileMapDragMode.East) {
                        Cursor = Cursors.SizeWE;
                    } else if (tileMapDragMode == TileMapDragMode.North || tileMapDragMode == TileMapDragMode.South) {
                        Cursor = Cursors.SizeNS;
                    } else if (tileMapDragMode == (TileMapDragMode.North | TileMapDragMode.West) || tileMapDragMode == (TileMapDragMode.South | TileMapDragMode.East)) {
                        Cursor = Cursors.SizeNWSE;
                    } else if (tileMapDragMode == (TileMapDragMode.North | TileMapDragMode.East) || tileMapDragMode == (TileMapDragMode.South | TileMapDragMode.West)) {
                        Cursor = Cursors.SizeNESW;
                    } else {
                        Cursor = Cursors.Default;
                    }
                }

                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left && tileMapDragMode != TileMapDragMode.None) {
                tileMapDragActive = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button == MouseButtons.Left) {
                tileMapDragActive = false;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            activeTilePos = default(Point);
            activeTileRect = default(Rect);

            Invalidate();
        }
    }
}