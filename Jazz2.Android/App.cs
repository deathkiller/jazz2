using System.Text;
using Android.App;

namespace Jazz2.Game
{
    public partial class App
    {
        public static string AssemblyTitle
        {
            get
            {
                return Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName, 0).ApplicationInfo.Name;
            }
        }

        public static string AssemblyVersion
        {
            get
            {
                return Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName, 0).VersionName;
            }
        }

        public static string AssemblyPath
        {
            get
            {
                return null;
            }
        }

        private static StringBuilder logBuffer = new StringBuilder();

        public static void Log(string message, params object[] messageParams)
        {
            string line = (messageParams != null && messageParams.Length > 0 ? string.Format(message, messageParams) : message);

            logBuffer.AppendLine(line);

#if DEBUG
            global::Android.Util.Log.Info("Jazz2", line);
#endif
        }

        public static string GetLogBuffer()
        {
            return logBuffer.ToString();
        }
    }
}