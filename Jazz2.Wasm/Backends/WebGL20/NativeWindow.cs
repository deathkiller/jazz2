using System;
using WebAssembly;

namespace Duality.Backend.Wasm
{
    public class NativeWindow : INativeWindow
    {
        private ScreenMode screenMode;
        private JSObject window;
        private Action<double> updateDelegate;

        public NativeWindow(WindowOptions options)
        {
            this.ScreenMode = options.ScreenMode;

            window = (JSObject)Runtime.GetGlobalObject();
            updateDelegate = new Action<double>(OnUpdate);

            // ToDo
            //DualityApp.Mouse.Source = new GameWindowMouseInputSource(this.internalWindow);
            DualityApp.Keyboard.Source = new KeyboardInputSource();
        }

        void IDisposable.Dispose()
        {
        }

        void INativeWindow.Run()
        {
            window.Invoke("requestAnimationFrame", updateDelegate);
        }

        public string Title
        {
            get
            {
                // ToDo
                return "";
            }
            set
            {
                // ToDo
            }
        }

        public Point2 Size
        {
            get { return Point2.Zero; }
            set { }
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
            //DualityApp.CalculateGameViewport(this.Size, out viewportRect, out imageSize);
            imageSize = new Vector2(720, 405);
            viewportRect = new Rect(720, 405);

            DualityApp.Render(null, viewportRect, imageSize);

            window.Invoke("requestAnimationFrame", updateDelegate);
        }
    }
}