using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Views;

namespace Jazz2.Android
{
    [Activity(//Label = "Jazz² Resurrection",
        MainLauncher = true,
        Icon = "@mipmap/ic_launcher",
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden,
        ScreenOrientation = ScreenOrientation.UserLandscape,
        LaunchMode = LaunchMode.SingleInstance
#if __ANDROID_11__
        , HardwareAccelerated = false
#endif
        )]
    public class MainActivity : Activity
    {
        private GLView view;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            CrashHandlerActivity.Register(this);

            Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            try {
                View decorView = Window.DecorView;
                decorView.SystemUiVisibility |= (StatusBarVisibility)SystemUiFlags.LayoutStable;
                decorView.SystemUiVisibility |= (StatusBarVisibility)SystemUiFlags.LayoutFullscreen;
                decorView.SystemUiVisibility |= (StatusBarVisibility)SystemUiFlags.Immersive;

                //if ((int)Build.VERSION.SdkInt < 18)
                //    RequestedOrientation = ScreenOrientation.SensorLandscape;

                Window.ClearFlags(WindowManagerFlags.TranslucentStatus);
                Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);

                if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop) {
                    Window.SetStatusBarColor(Color.Argb(0x22, 0x00, 0x00, 0x00));
                }
            } catch /*(Exception ex)*/ {
#if DEBUG
                throw;
#endif
            }

            // Create our OpenGL view, and display it
            view = new GLView(this);
            SetContentView(view);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnPause()
        {
            base.OnPause();
            view.Pause();
        }

        protected override void OnResume()
        {
            base.OnResume();
            view.Resume();
        }
    }
}