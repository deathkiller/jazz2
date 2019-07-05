using System;

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
            return null;
        }
    }
}