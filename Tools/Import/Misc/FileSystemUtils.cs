using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Import
{
    public static class FileSystemUtils
    {
        public static bool FileResolveCaseInsensitive(ref string path)
        {
            if (File.Exists(path)) {
                return true;
            }

            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory)) {
                return false;
            }

            string fileName = Path.GetFileName(path);
            string found = Directory.EnumerateFiles(directory).FirstOrDefault(current => string.Compare(Path.GetFileName(current), fileName, StringComparison.OrdinalIgnoreCase) == 0);
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

        public static bool FileTryDelete(string path)
        {
            for (int i = 0; i < 5; i++) {
                try {
                    File.Delete(path);
                    return true;
                } catch {
                    Thread.Sleep((i + 1) * 100);
                }
            }

            return false;
        }

        public static bool DirectoryTryDelete(string path, bool recursive)
        {
            for (int i = 0; i < 5; i++) {
                try {
                    Directory.Delete(path, recursive);
                    return true;
                } catch {
                    Thread.Sleep((i + 1) * 100);
                }
            }

            return false;
        }
    }
}