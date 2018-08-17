using Android.App;

namespace Jazz2.Game
{
    public partial class App
    {
        public static string AssemblyTitle
        {
            get
            {
                return Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName, 0).ApplicationInfo.Name;
            }
        }

        public static string AssemblyVersion
        {
            get
            {
                return Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName, 0).VersionName;
            }
        }

        public static void GetAssemblyVersionNumber(out byte major, out byte minor, out byte build)
        {
            string[] v = Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName, 0).VersionName.Split('.');
            major = byte.Parse(v[0]);
            minor = byte.Parse(v[1]);
            build = byte.Parse(v[2]);
        }
    }
}