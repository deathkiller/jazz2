using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;

namespace Import
{
    public class Utils
    {
        private static bool? isOutputRedirected;
        private static bool supportsUnicode;

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

        public static bool SupportsUnicode
        {
            get
            {
                return supportsUnicode;
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

                    supportsUnicode = true;
                } else {
                    // Multiple characters displayed, Unicode not supported
                    Console.OutputEncoding = prevEncoding;
                    Console.CursorLeft -= 3;
                    Console.Write("   ");
                    Console.CursorLeft -= 3;

                    supportsUnicode = false;
                }
            } else {
                supportsUnicode = true;
            }
        }

        public static bool FileResolveCaseInsensitive(ref string path)
        {
            if (File.Exists(path)) {
                return true;
            }

            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            string found = Directory.EnumerateFiles(directory).FirstOrDefault(current => string.Compare(Path.GetFileName(current), fileName, true) == 0);
            if (found == null) {
                return false;
            } else {
                path = found;
                return true;
            }
        }

        public static bool FileExistsCaseSensitive(string path)
        {
            path = path.Replace('/', Path.DirectorySeparatorChar);

            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                // Check case-sensitive on Windows
                if (File.Exists(path)) {
                    string directory = Path.GetDirectoryName(path);
                    string fileName = Path.GetFileName(path);
                    string found = Directory.EnumerateFiles(directory, fileName).First();
                    if (found == null || found == path) {

                        directory = directory.TrimEnd(Path.DirectorySeparatorChar);

                        while (true) {
                            int index = directory.LastIndexOf(Path.DirectorySeparatorChar);
                            if (index >= 0) {
                                string directoryName = directory.Substring(index + 1);
                                string parent = directory.Substring(0, index);

                                bool isDrive = (parent.Length == 2 && char.IsLetter(parent[0]) && parent[1] == ':');
                                if (isDrive) {
                                    // Parent directory is probably drive specifier (C:)
                                    // Append backslash...
                                    parent += Path.DirectorySeparatorChar;
                                }

                                found = Directory.EnumerateDirectories(parent, directoryName).First();
                                if (found != null && found != directory) {
                                    return false;
                                }

                                if (isDrive) {
                                    // Parent directory is probably drive specifier (C:)
                                    // Check is done...
                                    break;
                                }

                                directory = parent;
                            } else {
                                // No directory separator found
                                break;
                            }
                        }

                        return true;
                    }
                }

                return false;
            } else {
                return File.Exists(path);
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