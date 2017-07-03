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

            Window.AddFlags(WindowManagerFlags.KeepScreenOn);
            //Window.ClearFlags(WindowManagerFlags.TranslucentNavigation | WindowManagerFlags.TranslucentStatus);

            View decorView = Window.DecorView;
            decorView.SystemUiVisibility |= (StatusBarVisibility)SystemUiFlags.LayoutStable;
            decorView.SystemUiVisibility |= (StatusBarVisibility)SystemUiFlags.LayoutFullscreen;
            decorView.SystemUiVisibility |= (StatusBarVisibility)SystemUiFlags.Immersive;

            //if ((int)Build.VERSION.SdkInt < 18)
            //    RequestedOrientation = ScreenOrientation.SensorLandscape;

            Window.ClearFlags(WindowManagerFlags.TranslucentStatus);
            Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            Window.SetStatusBarColor(Color.Argb(0x22, 0x00, 0x00, 0x00));

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

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            return view.OnKeyDown(keyCode, e);
        }

        public override bool OnKeyUp(Keycode keyCode, KeyEvent e)
        {
            return view.OnKeyUp(keyCode, e);
        }
    }
}