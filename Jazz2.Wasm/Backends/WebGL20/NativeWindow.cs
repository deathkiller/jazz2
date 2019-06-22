using System;

namespace Duality.Backend.Android.OpenTK
{
    public class NativeWindow : INativeWindow
    {
        private ScreenMode screenMode;

        public NativeWindow(WindowOptions options)
        {
            this.ScreenMode = options.ScreenMode;
        }

        void IDisposable.Dispose()
        {
        }

        void INativeWindow.Run()
        {
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
    }
}