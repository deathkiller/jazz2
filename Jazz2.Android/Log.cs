using System;
using System.Text;

namespace Duality
{
    public enum LogType
    {
        Verbose,
        Info,
        Warning,
        Error
    }

    public static class Log
    {
        private static StringBuilder logBuffer = new StringBuilder();

        public static void PushIndent()
        {
            // TODO
        }

        public static void PopIndent()
        {
            // TODO
        }

        public static void Write(LogType type, string message)
        {
            if (string.IsNullOrEmpty(message)) {
                return;
            }

            logBuffer.AppendLine(message);

#if DEBUG
            global::Android.Util.Log.Info("Jazz2", message);
#endif
        }

        public static void Write(LogType type, string message, params object[] messageParams)
        {
            Write(type, messageParams != null && messageParams.Length > 0 ? string.Format(message, messageParams) : message);
        }

        internal static string GetLogBuffer()
        {
            return logBuffer.ToString();
        }
    }
}