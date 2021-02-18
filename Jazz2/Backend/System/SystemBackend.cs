using System;
using System.Runtime.InteropServices;

using Duality.IO;

namespace Duality.Backend.DotNetFramework
{
    public class SystemBackend : ISystemBackend
    {
        private NativeFileSystem fileSystem = new NativeFileSystem();

        string IDualityBackend.Id
        {
            get { return "DotNetFrameworkSystemBackend"; }
        }
        string IDualityBackend.Name
        {
            get { return ".NET Framework"; }
        }
        int IDualityBackend.Priority
        {
            get { return 0; }
        }

        IFileSystem ISystemBackend.FileSystem
        {
            get { return this.fileSystem; }
        }

        bool IDualityBackend.CheckAvailable()
        {
            return true;
        }
        void IDualityBackend.Init() { }
        void IDualityBackend.Shutdown() { }

        string ISystemBackend.GetNamedPath(NamedDirectory dir)
        {
            string path;
            switch (dir) {
                default: path = null; break;
                case NamedDirectory.Current: path = System.IO.Directory.GetCurrentDirectory(); break;
                case NamedDirectory.ApplicationData: path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); break;
                case NamedDirectory.MyDocuments: path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); break;
                case NamedDirectory.MyMusic: path = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic); break;
                case NamedDirectory.MyPictures: path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures); break;
                case NamedDirectory.SavedGames: path = GetSavedGamesFolderPath(); break;
            }

            if (path == null) {
                return null;
            }

            return this.fileSystem.GetDualityPathFormat(path);
        }

        public static string GetSavedGamesFolderPath()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) {
                return null;
            }

            try {
                IntPtr pPath;
                SHGetKnownFolderPath(/*FOLDERID_SavedGames*/new Guid("{4C5C32FF-BB9D-43b0-B5B4-2D72E54EAAA4}"), /*KF_FLAG_DEFAULT*/0, IntPtr.Zero, out pPath);
                string path = Marshal.PtrToStringUni(pPath);
                Marshal.FreeCoTaskMem(pPath);
                return path;
            } catch {
                return null;
            }
        }

        [DllImport("shell32")]
        private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr pszPath);
    }
}