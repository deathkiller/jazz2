using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Widget;

namespace Jazz2.Android
{
    [Activity(Label = "Jazz² Resurrection Crashed", Process = ":crash_handler")]
    public class CrashHandlerActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            TextView view = new TextView(this);
            view.SetPadding(40, 40, 40, 40);

            string exceptionData = Intent.GetStringExtra("ExceptionData");
            if (string.IsNullOrEmpty(exceptionData)) {
                exceptionData = "<i><b>Unknown error</b></i>";
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.N) {
                view.TextFormatted = Html.FromHtml(exceptionData, FromHtmlOptions.ModeCompact);
            } else {
                view.TextFormatted = Html.FromHtml(exceptionData);
            }

            SetContentView(view);
        }

        public static void Register(Activity activity)
        {
            AndroidEnvironment.UnhandledExceptionRaiser += (sender, e) => {
                try {
                    var context = activity.ApplicationContext;

                    StringBuilder sb = new StringBuilder();

                    sb.AppendLine("<big><font color=\"#000000\"><b>" + /*App.AssemblyTitle*/"Jazz² Resurrection" + "</b> has exited unexpectedly!</font></big>");
                    sb.AppendLine("<br><br><hr><small>");

                    Exception innerException = e.Exception.InnerException;
                    if (innerException != null) {
                        sb.Append("<b>");
                        sb.Append(WebUtility.HtmlEncode(innerException.Message));
                        sb.Append("</b> (");
                        sb.Append(WebUtility.HtmlEncode(e.Exception.Message));
                        sb.AppendLine(")<br>");

                        do {
                            ParseStackTrace(sb, innerException);
                            innerException = innerException.InnerException;
                        } while (innerException != null);
                    } else {
                        sb.Append("<b>");
                        sb.Append(WebUtility.HtmlEncode(e.Exception.Message));
                        sb.AppendLine("</b><br>");
                    }

                    ParseStackTrace(sb, e.Exception);

                    sb.AppendLine("</small>");
                    sb.AppendLine("<br><br>Please report this issue to developer (<a>https://github.com/deathkiller/jazz2</a>).");

                    Intent intent = new Intent(context, typeof(CrashHandlerActivity));
                    intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
                    intent.PutExtra("ExceptionData", sb.ToString());
                    context.StartActivity(intent);

                    activity.Finish();
                } catch {
                    // Nothing to do...
                }
            };
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

                    // Assembly.GetEntryAssembly() is null on Android
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
    }
}