using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
    }
}