using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Text;
using Android.Text.Method;
using Android.Views;
using Android.Widget;
using Jazz2.Game;
using AggregateException = System.AggregateException;

namespace Jazz2.Android
{
    [Activity(
        Icon = "@mipmap/ic_launcher",
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize,
        ScreenOrientation = ScreenOrientation.UserLandscape,
        ResizeableActivity = true
    )]
    public class CrashHandlerActivity : Activity
    {
        private static Activity currentActivity;

        public static void Register(Activity activity)
        {
            currentActivity = activity;

            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
            AndroidEnvironment.UnhandledExceptionRaiser += OnUnhandledExceptionRaiser;
        }

        private static void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ShowErrorDialog(e.ExceptionObject as Exception);
        }

        private static void OnUnhandledExceptionRaiser(object sender, RaiseThrowableEventArgs e)
        {
            ShowErrorDialog(e.Exception);
            e.Handled = true;
        }

        public static void ShowErrorDialog(Exception ex)
        {
            AppDomain.CurrentDomain.UnhandledException -= OnCurrentDomainUnhandledException;
            AndroidEnvironment.UnhandledExceptionRaiser -= OnUnhandledExceptionRaiser;

            try {
                StringBuilder sb = new StringBuilder();
                StringBuilder sbStacktrace = new StringBuilder();
                string title, message;

                // Obtain debugging information
                Exception innerException = ex.InnerException;
                if (innerException != null) {
                    sb.Append("<b>");
                    sb.Append(WebUtility.HtmlEncode(innerException.Message).Replace("\n", "<br>"));
                    sb.Append("</b><br>");
                    sb.Append(WebUtility.HtmlEncode(ex.Message).Replace("\n", "<br>"));
                    sb.AppendLine("<br>");

                    title = innerException.GetType().FullName + " {[" + ex.GetType().FullName + "]}";
                    message = innerException.Message + " {[" + ex.Message + "]}";

                    do {
                        ParseStackTrace(sbStacktrace, innerException);
                        innerException = innerException.InnerException;
                    } while (innerException != null);
                } else {
                    sb.Append("<b>");
                    sb.Append(WebUtility.HtmlEncode(ex.Message).Replace("\n", "<br>"));
                    sb.AppendLine("</b><br>");

                    title = ex.GetType().FullName;
                    message = ex.Message;
                }

                ParseStackTrace(sbStacktrace, ex);

                // Start new activity in separate process
                Context context = currentActivity;
                Intent intent = new Intent(context, typeof(CrashHandlerActivity));
                intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask | ActivityFlags.ClearTop);
                intent.PutExtra("ExceptionData", sb.ToString());

                // Remove some formatting from stacktrace before sending
                sbStacktrace.Replace(" • ", "- ").Replace("<br>", "");

                intent.PutExtra("Title", title);
                intent.PutExtra("Message", message);
                intent.PutExtra("Stacktrace", sbStacktrace.ToString());
                intent.PutExtra("Log", App.GetLogBuffer());

                PendingIntent pendingIntent = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.OneShot);

                AlarmManager mgr = (AlarmManager)context.GetSystemService(Context.AlarmService);
                mgr.Set(AlarmType.Rtc, Java.Lang.JavaSystem.CurrentTimeMillis() + 200, pendingIntent);

                // Try to close current activity
                Activity activity = currentActivity;
                if (activity != null) {
                    activity.Finish();
                }

                // ToDo
                //android.os.Process.killProcess(android.os.Process.myPid());
                Java.Lang.JavaSystem.Exit(2);
            } catch (Exception ex2) {
#if DEBUG
                Console.WriteLine("CrashHandlerActivity.ShowErrorDialog() failed: " + ex2);
#endif
            }
        }

        private static void ParseStackTrace(StringBuilder sb, Exception ex)
        {
            StackTrace trace = new StackTrace(ex, true);

            for (int i = 0; i < trace.FrameCount; i++) {
                sb.AppendLine("<br>");

                StackFrame frame = trace.GetFrame(i);
                MethodBase method = frame.GetMethod();
                if (method == null) {
                    continue;
                }

                Type type = method.DeclaringType;

                bool isPInvoke = (method.Attributes & MethodAttributes.PinvokeImpl) != 0;
                bool isInternalCall = (method.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) != 0;

                string filename = frame.GetFileName();
                if (string.IsNullOrEmpty(filename)) {
                    sb.Append("<font color=\"#666666\"> • <i>");
                    sb.Append(WebUtility.HtmlEncode(type?.FullName));
                    sb.Append(".</i><b>");
                    sb.Append(WebUtility.HtmlEncode(method.Name));
                    sb.Append("</b>");

                    if (frame.GetILOffset() == -1) {
                        if (isPInvoke) {
                            sb.Append(" (<i>extern</i>)");
                        } else if (isInternalCall) {
                            sb.Append(" (<i>internal</i>)");
                        }
                    } else {
                        sb.Append(" (0x");
                        sb.Append(frame.GetILOffset().ToString("X2"));
                        sb.Append(")");
                    }

                    sb.Append("</font>");
                } else {
                    int i1 = filename.LastIndexOfAny(new[] { '/', '\\' });
                    if (i1 != -1) {
                        int i2 = filename.LastIndexOfAny(new[] { '/', '\\' }, i1 - 1);
                        if (i2 != -1) {
                            int i3 = filename.LastIndexOfAny(new[] { '/', '\\' }, i2 - 1);
                            if (i3 != -1) {
                                filename = "…" + filename.Substring(i3);
                            }
                        }
                    }

                    sb.Append(" • <i>");
                    sb.Append(WebUtility.HtmlEncode(type?.FullName));
                    sb.Append(".</i><b>");
                    sb.Append(WebUtility.HtmlEncode(method.Name));
                    sb.Append("</b> (");
                    sb.Append(WebUtility.HtmlEncode(filename));
                    sb.Append(":");
                    sb.Append(frame.GetFileLineNumber());
                    sb.Append(")");
                }
            }
        }

        private string title;
        private string message;
        private string stacktrace;
        private string log;

        private VideoView backgroundVideo;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            title = Intent.GetStringExtra("Title") ?? "";
            message = Intent.GetStringExtra("Message") ?? "";
            stacktrace = Intent.GetStringExtra("Stacktrace") ?? "";
            log = Intent.GetStringExtra("Log") ?? "";

            string exceptionData = Intent.GetStringExtra("ExceptionData");
            if (string.IsNullOrEmpty(exceptionData)) {
                exceptionData = "Cannot receive information about this failure.<br><br><small>If you have any issues, report it to developers.<br><a href=\"https://github.com/deathkiller/jazz2\">https://github.com/deathkiller/jazz2</a></small>";
            } else {
                exceptionData += "<br><br><small>This report was sent to developers to help resolve it.<br><a href=\"https://github.com/deathkiller/jazz2\">https://github.com/deathkiller/jazz2</a></small>";
            }

            try {
                View decorView = Window.DecorView;
                decorView.SystemUiVisibility |= (StatusBarVisibility)SystemUiFlags.LayoutStable;
                decorView.SystemUiVisibility |= (StatusBarVisibility)SystemUiFlags.LayoutFullscreen;

                Window.ClearFlags(WindowManagerFlags.TranslucentStatus);
                Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);

                if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop) {
                    Window.SetStatusBarColor(new Color(0x30000000));
                }
            } catch {
                // Nothing to do...
            }

            SetContentView(Resource.Layout.activity_info);

            backgroundVideo = FindViewById<VideoView>(Resource.Id.background_video);
            backgroundVideo.SetVideoURI(global::Android.Net.Uri.Parse("android.resource://" + PackageName + "/raw/logo"));
            backgroundVideo.Prepared += OnVideoViewPrepared;
            backgroundVideo.Start();

            Button closeButton = FindViewById<Button>(Resource.Id.retry_button);
            closeButton.Visibility = ViewStates.Gone;

            TextView versionView = FindViewById<TextView>(Resource.Id.version);
            versionView.Text = "v" + App.AssemblyVersion;

            TextView headerView = FindViewById<TextView>(Resource.Id.header);
            headerView.Text = "Application has exited unexpectedly";

            TextView contentView = FindViewById<TextView>(Resource.Id.content);
            contentView.MovementMethod = LinkMovementMethod.Instance;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.N) {
                contentView.TextFormatted = Html.FromHtml(exceptionData, FromHtmlOptions.ModeLegacy);
            } else {
                contentView.TextFormatted = Html.FromHtml(exceptionData);
            }

            // Send report
            if (!string.IsNullOrEmpty(title)) {
                new Task(SendReportAsync).Start();
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (backgroundVideo != null) {
                backgroundVideo.Start();
            }
        }

        private void OnVideoViewPrepared(object sender, EventArgs e)
        {
            ((global::Android.Media.MediaPlayer)sender).Looping = true;
        }

        private async void SendReportAsync()
        {
            const string url = "http://deat.tk/crash-reports/api/report";
            const string secret = "1:2zsfnWzBkPyEIFEhB2MSr2TyTgrLghL7wXYdSTOe";

            string appVersion;
            try {
                appVersion = App.AssemblyVersion ?? "unknown";
            } catch {
                appVersion = "unknown";
            }

            string device;
            try {
                device = (string.IsNullOrEmpty(Build.Model) ? Build.Manufacturer : (Build.Model.StartsWith(Build.Manufacturer) ? Build.Model : Build.Manufacturer + " " + Build.Model));

                if (device == null) {
                    device = "unknown";
                } else if (device.Length > 1) {
                    device = char.ToUpper(device[0]) + device.Substring(1);
                }
            } catch {
                device = "unknown";
            }

            string deviceId;
            try {
                deviceId = Settings.Secure.GetString(ContentResolver, Settings.Secure.AndroidId);
                if (string.IsNullOrWhiteSpace(deviceId)) {
                    deviceId = "";
                }
            } catch {
                deviceId = "";
            }

            try {
                FormUrlEncodedContent content = new FormUrlEncodedContent(new KeyValuePair<string, string>[] {
                    new KeyValuePair<string, string>("secret", secret),

                    new KeyValuePair<string, string>("type", "crash"),
                    new KeyValuePair<string, string>("app_version", appVersion),
                    new KeyValuePair<string, string>("app_target", "android"),
#if DEBUG
                    new KeyValuePair<string, string>("app_configuration", "debug"),
#else
                    new KeyValuePair<string, string>("app_configuration", "release"),
#endif
                    new KeyValuePair<string, string>("title", title),
                    new KeyValuePair<string, string>("message", message),
                    new KeyValuePair<string, string>("stacktrace", stacktrace),
                    new KeyValuePair<string, string>("log", log),
                    new KeyValuePair<string, string>("os", "Android " + Build.VERSION.Release),
                    new KeyValuePair<string, string>("additional_data", "{\"device\":\"" + device.Replace("\"", "\\\"") + "\",\"device_id\":\"" + deviceId.Replace("\"", "\\\"") + "\"}"),
                });

                var client = new HttpClient();

                var result = await client.PostAsync(url, content);

#if DEBUG
                Console.WriteLine(result.StatusCode);
                Console.WriteLine(result.Content);
#endif
            } catch (AggregateException ex) {
#if DEBUG
                foreach (var item in ex.Flatten().InnerExceptions) {
                    Console.WriteLine(item.Message);
                }
#endif
            }
        }
    }
}