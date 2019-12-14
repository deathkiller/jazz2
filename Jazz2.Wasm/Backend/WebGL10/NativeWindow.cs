using System;
using WebAssembly;

namespace Duality.Backend.Wasm.WebGL10
{
    public class NativeWindow : INativeWindow
    {
        private ScreenMode screenMode;
        private JSObject window;
        private Action<double> updateDelegate;

        public NativeWindow(WindowOptions options)
        {
            this.ScreenMode = options.ScreenMode;

            window = (JSObject)Runtime.GetGlobalObject("window");
            updateDelegate = new Action<double>(OnUpdate);

            // ToDo
            //DualityApp.Mouse.Source = new GameWindowMouseInputSource(this.internalWindow);
            DualityApp.Keyboard.Source = new KeyboardInputSource();
        }

        void IDisposable.Dispose()
        {
            if (window != null) {
                window.Dispose();
                window = null;
            }
        }

        void INativeWindow.Run()
        {
            window.Invoke("requestAnimationFrame", updateDelegate);
        }

        public string Title
        {
            get
            {
                using (var document = (JSObject)Runtime.GetGlobalObject("document")) {
                    return (string)document.GetObjectProperty("title");
                }
            }
            set
            {
                using (var document = (JSObject)Runtime.GetGlobalObject("document")) {
                    document.SetObjectProperty("title", value);
                }
            }
        }

        public Point2 Size
        {
            get { return GraphicsBackend.GetCanvasSize(); }
            set { GraphicsBackend.SetCanvasSize(value); }
        }

        public RefreshMode RefreshMode
        {
            get { return RefreshMode.VSync; }
            set
            {
                // Only VSync is supported
                if (value == RefreshMode.VSync) {
                    return;
                }
                
                throw new NotSupportedException();
            }
        }
        
        public ScreenMode ScreenMode
        {
            get { return screenMode; }
            set
            {
                // ToDo
            }
        }

        private void OnUpdate(double milliseconds)
        {
            if (DualityApp.ExecContext == DualityApp.ExecutionContext.Terminated) {
                return;
            }

            DualityApp.Update();

            Vector2 imageSize;
            Rect viewportRect;
            DualityApp.CalculateGameViewport(this.Size, out viewportRect, out imageSize);

            DualityApp.Render(null, viewportRect, imageSize);

            window.Invoke("requestAnimationFrame", updateDelegate);
        }
    }
}