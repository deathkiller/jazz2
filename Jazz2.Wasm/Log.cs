using System;

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

            Console.WriteLine(message);
        }

        public static void Write(LogType type, string message, params object[] messageParams)
        {
            Write(type, messageParams != null && messageParams.Length > 0 ? string.Format(message, messageParams) : message);
        }
    }
}
