using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Text.Method;
using Android.Widget;

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

                // Append simple header
                // ToDo: Remove this hardcoded title
                sb.AppendLine("<big><font color=\"#000000\"><b>" + /*App.AssemblyTitle*/"Jazz² Resurrection" + "</b> has exited unexpectedly!</font></big>");
                sb.AppendLine("<br><br><hr><small>");

                // Obtain debugging information
                Exception innerException = ex.InnerException;
                if (innerException != null) {
                    sb.Append("<b>");
                    sb.Append(WebUtility.HtmlEncode(innerException.Message).Replace("\n", "<br>"));
                    sb.Append("</b> (");
                    sb.Append(WebUtility.HtmlEncode(ex.Message).Replace("\n", "<br>"));
                    sb.AppendLine(")<br>");

                    do {
                        ParseStackTrace(sb, innerException);
                        innerException = innerException.InnerException;
                    } while (innerException != null);
                } else {
                    sb.Append("<b>");
                    sb.Append(WebUtility.HtmlEncode(ex.Message).Replace("\n", "<br>"));
                    sb.AppendLine("</b><br>");
                }

                ParseStackTrace(sb, ex);

                // Append additional information
                sb.AppendLine("</small>");
                sb.AppendLine("<br><br>Please report this issue to developer.<br><a href=\"https://github.com/deathkiller/jazz2\">https://github.com/deathkiller/jazz2</a>");

                // Start new activity in separate process
                //Context context = Application.Context;
                //Context context = DualityActivity.Current.ApplicationContext;
                Context context = currentActivity;
                Intent intent = new Intent(context, typeof(CrashHandlerActivity));
                intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask | ActivityFlags.ClearTop);
                intent.PutExtra("ExceptionData", sb.ToString());
                //context.StartActivity(intent);

                PendingIntent pendingIntent = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.OneShot);

                AlarmManager mgr = (AlarmManager)context.GetSystemService(Context.AlarmService);
                mgr.Set(AlarmType.Rtc, Java.Lang.JavaSystem.CurrentTimeMillis() + 200, pendingIntent);

                // Try to close current activity
                Activity activity = currentActivity;
                if (activity != null) {
                    activity.Finish();
                }

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
        }
    }
}