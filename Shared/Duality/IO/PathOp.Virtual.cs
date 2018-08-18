using System;
using System.Collections.Generic;

namespace Duality.IO
{
    static partial class PathOp
    {
        private struct VirtualFileSystem
        {
            public string Prefix;
            public IFileSystem FileSystem;
        }

        private static RawList<VirtualFileSystem> virtualFileSystems = new RawList<VirtualFileSystem>();

        public static void Mount(string prefix, IFileSystem fileSystem)
        {
            //if (prefix[prefix.Length - 1] != DirectorySeparatorChar) {
            //    prefix = prefix + DirectorySeparatorChar;
            //}

            for (int i = 0; i < virtualFileSystems.Count; i++) {
                if (virtualFileSystems[i].Prefix == prefix) {
                    throw new InvalidOperationException();
                }
            }

            virtualFileSystems.Add(new VirtualFileSystem {
                Prefix = prefix,
                FileSystem = fileSystem
            });
        }

        public static void Unmount(string prefix)
        {
            //if (prefix[prefix.Length - 1] != DirectorySeparatorChar) {
            //    prefix = prefix + DirectorySeparatorChar;
            //}

            for (int i = 0; i < virtualFileSystems.Count; i++) {
                if (virtualFileSystems[i].Prefix == prefix) {
                    IDisposable disposable = virtualFileSystems[i].FileSystem as IDisposable;
                    if (disposable != null) {
                        disposable.Dispose();
                    }

                    virtualFileSystems.RemoveAt(i);
                    break;
                }
            }
        }

        internal static IFileSystem ResolveFileSystem(ref string path)
        {
            for (int i = 0; i < virtualFileSystems.Count; i++) {
                int length = virtualFileSystems.Data[i].Prefix.Length;

                if (path.StartsWith(virtualFileSystems.Data[i].Prefix, StringComparison.Ordinal) &&
                    path.Length > length &&
                    (path[length] == DirectorySeparatorChar ||
                     path[length] == AltDirectorySeparatorChar)) {

                    path = path.Substring(virtualFileSystems.Data[i].Prefix.Length + 1);
                    return virtualFileSystems.Data[i].FileSystem;
                }
            }

            return DualityApp.SystemBackend.FileSystem;
        }

        internal static int IndexOfFileSystem(string path)
        {
            for (int i = 0; i < virtualFileSystems.Count; i++) {
                int length = virtualFileSystems.Data[i].Prefix.Length;

                if (path.StartsWith(virtualFileSystems.Data[i].Prefix, StringComparison.Ordinal) &&
                    path.Length > length &&
                    (path[length] == DirectorySeparatorChar ||
                     path[length] == AltDirectorySeparatorChar)) {

                    return i;
                }
            }

            return -1;
        }

        internal static IEnumerable<string> PrepareFileSystemForEnumerationUnsafe(int index, bool directories, string path, bool recursive)
        {
            string prefix = virtualFileSystems.Data[index].Prefix;

            path = path.Substring(prefix.Length + 1);

            IEnumerable<string> items;
            if (directories) {
                items = virtualFileSystems.Data[index].FileSystem.GetDirectories(path, recursive);
            } else {
                items = virtualFileSystems.Data[index].FileSystem.GetFiles(path, recursive);
            }

            foreach (string item in items) {
                yield return Combine(prefix, item);
            }
        }

        internal static void UnmountAll()
        {
            for (int i = 0; i < virtualFileSystems.Count; i++) {
                IDisposable disposable = virtualFileSystems[i].FileSystem as IDisposable;
                if (disposable != null) {
                    disposable.Dispose();
                }
            }

            virtualFileSystems.Clear();
        }
    }
}