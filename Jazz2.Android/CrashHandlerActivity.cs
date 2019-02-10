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
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Text;
using Android.Text.Method;
using Android.Widget;
using Jazz2.Game;
using AggregateException = System.AggregateException;

namespace Jazz2.Android
{
    [Activity(Label = "Jazz² Resurrection Crashed"/*, Process = ":crash_handler"*/)]
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

                // Append simple header
                // ToDo: Remove this hardcoded title
                sb.AppendLine("<big><font color=\"#000000\"><b>" + /*App.AssemblyTitle*/"Jazz² Resurrection" + "</b> has exited unexpectedly!</font></big>");
                sb.AppendLine("<br><br><hr><small>");

                string title, message;

                // Obtain debugging information
                Exception innerException = ex.InnerException;
                if (innerException != null) {
                    sb.Append("<b>");
                    sb.Append(WebUtility.HtmlEncode(innerException.Message).Replace("\n", "<br>"));
                    sb.Append("</b> (");
                    sb.Append(WebUtility.HtmlEncode(ex.Message).Replace("\n", "<br>"));
                    sb.AppendLine(")<br>");

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

                sb.Append(sbStacktrace);

                // Append additional information
                sb.AppendLine("</small>");
                //sb.AppendLine("<br><br>Please report this issue to developer.<br><a href=\"https://github.com/deathkiller/jazz2\">https://github.com/deathkiller/jazz2</a>");
                sb.AppendLine("<br><br>This report was sent to developer. You can check state of the issue on: <a href=\"https://github.com/deathkiller/jazz2\">https://github.com/deathkiller/jazz2</a>");

                // Start new activity in separate process
                //Context context = Application.Context;
                //Context context = DualityActivity.Current.ApplicationContext;
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

                //context.StartActivity(intent);

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
                Console.WriteLine("CrashHandlerActivity failed: " + ex2);
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
                    //sb.Append("<font color=\"#666666\"> • Unknown method</font>");
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
                            filename = "..." + filename.Substring(i2);
                        }
                    }

                    // Assembly.GetEntryAssembly() is always null on Android
                    //bool isEntry = (type != null && type.Assembly == Assembly.GetExecutingAssembly());
                    bool isEntry = false;

                    sb.Append(" • ");

                    if (isEntry) {
                        sb.Append("<font color=\"#AF5C08\">");
                    }

                    sb.Append("<i>");

                    sb.Append(WebUtility.HtmlEncode(type?.FullName));
                    sb.Append(".</i><b>");
                    sb.Append(WebUtility.HtmlEncode(method.Name));
                    sb.Append("</b>");

                    if (isEntry) {
                        sb.Append("</font>");
                    }

                    sb.Append(" (");
                    sb.Append(frame.GetFileLineNumber());
                    sb.Append(":");
                    sb.Append(frame.GetFileColumnNumber());
                    sb.Append(" v ");
                    sb.Append(WebUtility.HtmlEncode(filename));
                    sb.Append(")");
                }
            }
        }

        private string title;
        private string message;
        private string stacktrace;
        private string log;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            try {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop) {
                    Window.SetStatusBarColor(new Color(0, 0, 0, 80));
                }
            } catch {
                // Nothing to do...
            }

            title = Intent.GetStringExtra("Title");
            message = Intent.GetStringExtra("Message");
            stacktrace = Intent.GetStringExtra("Stacktrace");
            log = Intent.GetStringExtra("Log");

            // Show simple view with debugging information
            TextView view = new TextView(this);
            view.SetPadding(40, 40, 40, 40);

            string exceptionData = Intent.GetStringExtra("ExceptionData");
            if (string.IsNullOrEmpty(exceptionData)) {
                exceptionData = "<i><b>Unknown error</b></i>";
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.N) {
                view.TextFormatted = Html.FromHtml(exceptionData, FromHtmlOptions.ModeLegacy);
            } else {
                #pragma warning disable CS0618
                view.TextFormatted = Html.FromHtml(exceptionData);
                #pragma warning restore CS0618
            }

            view.MovementMethod = LinkMovementMethod.Instance;

            SetContentView(view);

            // Send report
            new Task(SendReport).Start();
        }

        private async void SendReport()
        {
            const string uri = "http://deat.tk/crash-reports/api/report";
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
                deviceId = Settings.Secure.GetString(ContentResolver, Settings.Secure.AndroidId) ?? "";
            } catch {
                deviceId = "";
            }

            try {
                FormUrlEncodedContent content = new FormUrlEncodedContent(new KeyValuePair<string, string>[] {
                    new KeyValuePair<string, string>("secret", secret),

                    new KeyValuePair<string, string>("type", "crash"),
                    new KeyValuePair<string, string>("app_version", appVersion),
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

                var result = await client.PostAsync(uri, content);

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