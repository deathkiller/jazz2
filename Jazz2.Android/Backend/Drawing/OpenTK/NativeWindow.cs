using System;
using Android.Views;
using Duality.Android;

namespace Duality.Backend.Android.OpenTK
{
    public class NativeWindow : INativeWindow
    {
        private ScreenMode screenMode;

        public NativeWindow(WindowOptions options)
        {
            ((INativeWindow)this).ScreenMode = options.ScreenMode;
        }

        void IDisposable.Dispose()
        {
        }

        void INativeWindow.Run()
        {
            // DualityActivity runs automatically
        }

        string INativeWindow.Title
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

        ScreenMode INativeWindow.ScreenMode
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