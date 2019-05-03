using System;
using Android.Views;
using Jazz2.Android;

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
            // Activity runs automatically
        }

        public string Title
        {
            get
            {
                MainActivity activity = MainActivity.Current;
                if (activity != null) {
                    return activity.Title;
                } else {
                    return null;
                }
            }
            set
            {
                MainActivity activity = MainActivity.Current;
                if (activity != null) {
                    activity.Title = value;
                }
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
                // Only VSync is supported on Android
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
                // Android supports only Immersive flag
                value &= (ScreenMode.Immersive);
                if (screenMode == value) {
                    return;
                }

                MainActivity activity = MainActivity.Current;
                if (activity != null) {
                    if ((value & ScreenMode.Immersive) != 0) {
                        activity.Window.ClearFlags(WindowManagerFlags.Fullscreen);
                    } else {
                        activity.Window.AddFlags(WindowManagerFlags.Fullscreen);
                    }

                    screenMode = value;
                }
            }
        }
    }
}