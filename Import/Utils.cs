using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Import
{
    public class Utils
    {
        private static bool? isOutputRedirected;

        public static bool IsOutputRedirected
        {
            get
            {
                if (isOutputRedirected == null) {
                    try {
                        if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                            uint mode;
                            IntPtr hConsole = GetStdHandle(-11 /*STD_OUTPUT_HANDLE*/);
                            isOutputRedirected = GetFileType(hConsole) != 0x02 /*FILE_TYPE_CHAR*/ || !GetConsoleMode(hConsole, out mode);
                        } else {
                            isOutputRedirected = (isatty(1 /*stdout*/) == 0);
                        }
                    } catch {
                        // Nothing to do...
                        isOutputRedirected = false;
                    }
                }

                return isOutputRedirected == true;
            }
        }

        public static void TryEnableUnicode()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT && !IsOutputRedirected) {
                var prevEncoding = Console.OutputEncoding;
                int x = Console.CursorLeft;
                Console.OutputEncoding = Encoding.Unicode;
                Console.Write("Ω");
                if (Console.CursorLeft == x + 1) {
                    // One character displayed
                    Console.CursorLeft--;
                    Console.Write(" ");
                    Console.CursorLeft--;
                } else {
                    // Multiple characters displayed, Unicode not supported
                    Console.OutputEncoding = prevEncoding;
                    Console.CursorLeft -= 3;
                    Console.Write("   ");
                    Console.CursorLeft -= 3;
                }
            }
        }

        #region Native Methods
        [DllImport("libc")]
        private static extern int isatty(int desc);

        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32")]
        private static extern uint GetFileType(IntPtr hFile);

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsole, out uint lpMode);
        #endregion
    }
}