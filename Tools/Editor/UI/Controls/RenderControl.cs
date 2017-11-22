using System;
using System.Drawing;
using System.Windows.Forms;
using Duality;
using Duality.Drawing;
using Duality.Editor.Backend;

namespace Editor.UI.Controls
{
    public abstract class RenderControl : Control
    {
        private INativeRenderableSite graphicsControl;
        private DrawDevice device;
        private Canvas canvas;

        public RenderControl()
        {
            device = new DrawDevice();

            device.ClearColor = ColorRgba.Black;
            device.ClearFlags |= ClearFlag.All;
            device.FarZ = 1000;
            device.NearZ = 0;
            device.Perspective = PerspectiveMode.Flat;
            device.RenderMode = RenderMatrix.ScreenSpace;
            device.VisibilityMask = VisibilityFlag.All;

            canvas = new Canvas();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (!base.DesignMode) {
                InitGLControl();
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);

        }

        private void InitGLControl()
        {
            this.SuspendLayout();

            // Get rid of a possibly existing old glControl
            if (this.graphicsControl != null) {
                Control oldControl = this.graphicsControl.Control;

                oldControl.Paint -= RenderableControl_Paint;

                this.graphicsControl.Dispose();
                this.Controls.Remove(oldControl);
            }

            // Create a new glControl
            this.graphicsControl = App.CreateRenderableSite();
            if (this.graphicsControl == null) {
                return;
            }

            Control control = this.graphicsControl.Control;

            control.BackColor = Color.Black;
            control.Dock = DockStyle.Fill;
            control.Name = "graphicsControl";
            control.AllowDrop = true;

            control.Paint += RenderableControl_Paint;

            this.Controls.Add(control);
            this.Controls.SetChildIndex(control, 0);

            this.ResumeLayout(true);
        }

        private void RenderableControl_Paint(object sender, PaintEventArgs e)
        {
            if (DualityApp.ExecContext == DualityApp.ExecutionContext.Terminated) return;
            if (DualityApp.GraphicsBackend == null) return;

            // Retrieve OpenGL context
            try { this.graphicsControl.MakeCurrent(); } catch (Exception) { return; }

            // Perform rendering
            Size size = graphicsControl.Control.ClientSize;
            device.TargetSize = new Vector2(size.Width, size.Height);
            device.ViewportRect = new Rect(device.TargetSize);

            device.PrepareForDrawcalls();

            canvas.Begin(device);

            this.OnRender(canvas);

            canvas.End();

            device.Render();

            // Make sure the rendered result ends up on screen
            this.graphicsControl.SwapBuffers();
        }

        protected abstract void OnRender(Canvas canvas);

        protected void InvalidateView()
        {
            graphicsControl.Control.Invalidate();
        }
    }
}