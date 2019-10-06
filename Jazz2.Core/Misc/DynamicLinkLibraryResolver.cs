using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Duality;

namespace Jazz2
{
    public class DynamicLinkLibraryResolver : Disposable
    {
        private IntPtr hModule;

        public IntPtr Handle
        {
            get { return hModule; }
        }

        public DynamicLinkLibraryResolver(string name)
        {
#if PLATFORM_ANDROID
            const PlatformID platform = PlatformID.Unix;
            const string extension = ".so";

            string path = name + extension;
#elif PLATFORM_WASM
            throw new NotSupportedException("DynamicLinkLibraryResolver is not supported on this platform");

            const PlatformID platform = PlatformID.Unix;
            const string extension = ".so";

            string path = name + extension;
#else
            PlatformID platform = Environment.OSVersion.Platform;

            string extension;
            switch (platform) {
                default:
                case PlatformID.Win32NT: extension = ".dll"; break;
                case PlatformID.Unix: extension = ".so"; break;
                case PlatformID.MacOSX: extension = ".dylib"; break;
            }

            Assembly execAssembly = Assembly.GetEntryAssembly() ?? typeof(DualityApp).Assembly;
            string execAssemblyDir = Path.GetFullPath(Path.GetDirectoryName(execAssembly.Location));
            string path = Path.Combine(execAssemblyDir, DualityApp.PluginDirectory, Environment.Is64BitProcess ? "x64" : "x86", name + extension);
#endif

            switch (platform) {
                default:
                case PlatformID.Win32NT:
                    hModule = LoadLibraryEx(path, IntPtr.Zero, 0x00000008 /*LOAD_WITH_ALTERED_SEARCH_PATH*/);

                    if (hModule == IntPtr.Zero)
                        throw new Win32Exception("LoadLibraryEx(\"" + name + extension + "\") returned NULL");
                    break;
                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    // TODO: how can we read name remaps out of app.confg <dllmap> ?
                    hModule = dlopen(path, 2 /*RTLD_NOW*/);

                    if (hModule == IntPtr.Zero)
                        throw new Win32Exception("dlopen(\"" + name + extension + "\") returned NULL");
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (hModule != IntPtr.Zero) {
#if PLATFORM_ANDROID
                const PlatformID platform = PlatformID.Unix;
#elif PLATFORM_WASM
                throw new NotSupportedException("DynamicLinkLibraryResolver is not supported on this platform");

                const PlatformID platform = PlatformID.Unix;
#else
                PlatformID platform = Environment.OSVersion.Platform;
#endif

                switch (platform) {
                    default:
                    case PlatformID.Win32NT:
                        FreeLibrary(hModule);
                        break;
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        dlclose(hModule);
                        break;
                }

                hModule = IntPtr.Zero;
            }
        }

        public IntPtr Resolve(string procedureName)
        {
#if PLATFORM_ANDROID
            const PlatformID platform = PlatformID.Unix;
#elif PLATFORM_WASM
            throw new NotSupportedException("DynamicLinkLibraryResolver is not supported on this platform");

            const PlatformID platform = PlatformID.Unix;
#else
            PlatformID platform = Environment.OSVersion.Platform;
#endif

            switch (platform) {
                default:
                case PlatformID.Win32NT:
                    return GetProcAddress(hModule, procedureName);

                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    return dlsym(hModule, procedureName);
            }
        }

        public T Resolve<T>(string procedureName) where T : class
        {
            IntPtr ptr = Resolve(procedureName);
            return (ptr == IntPtr.Zero ? null : Marshal.GetDelegateForFunctionPointer(ptr, typeof(T)) as T);
        }

        #region Native Methods
        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, uint dwFlags);
        [DllImport("kernel32")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
        [DllImport("kernel32")]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("libdl.so")]
        private static extern IntPtr dlopen(string filename, int flags);
        [DllImport("libdl.so")]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);
        [DllImport("libdl.so")]
        private static extern int dlclose(IntPtr handle);
        #endregion
    }
}