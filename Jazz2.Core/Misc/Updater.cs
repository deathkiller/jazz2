using System.Net;
using System.Text;
using System.Threading;
using Jazz2.Game;

namespace Jazz2
{
    public class Updater
    {
        public delegate void CheckUpdatesCallback(bool newAvailable, string version);

#if __ANDROID__
        private const string Url = "http://deat.tk/downloads/android/jazz2/updates";
#else
        private const string Url = "http://deat.tk/downloads/games/jazz2/updates";
#endif

        public static void CheckUpdates(CheckUpdatesCallback callback)
        {
            if (callback == null) {
                return;
            }

            ThreadPool.UnsafeQueueUserWorkItem(delegate {
                string deviceId;
#if __ANDROID__
                try {
                    deviceId = global::Android.Provider.Settings.Secure.GetString(Android.MainActivity.Current.ContentResolver, global::Android.Provider.Settings.Secure.AndroidId);
                    if (deviceId == null) {
                        deviceId = "";
                    }
                } catch {
                    deviceId = "";
                }

                deviceId += "|Android " + global::Android.OS.Build.VERSION.Release;
#else
                try {
                    deviceId = System.Environment.MachineName;
                    if (deviceId == null) {
                        deviceId = "";
                    }
                } catch {
                    deviceId = "";
                }

                deviceId += "|" + System.Environment.OSVersion.ToString();
#endif

                deviceId = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(deviceId))
                                .Replace('+', '-').Replace('/', '_').TrimEnd('=');

                try {
                    string currentVersion = App.AssemblyVersion;

                    WebClient client = new WebClient();
                    client.Encoding = Encoding.UTF8;
                    client.Headers["User-Agent"] = App.AssemblyTitle;

                    string content = client.DownloadString(Url + "?v=" + currentVersion + "&d=" + deviceId);
                    if (content == null) {
                        callback(false, null);
                        return;
                    }

                    bool isNewer = IsVersionNewer(currentVersion, content);
                    callback(isNewer, content);
                } catch {
                    // Nothing to do...
                    callback(false, null);
                }
            }, null);
        }

        private static bool IsVersionNewer(string currentVersion, string newVersion)
        {
            int majorCurrent = 0, majorNew = 0;
            int minorCurrent = 0, minorNew = 0;
            int buildCurrent = 0, buildNew = 0;
            int revCurrent = 0, revNew = 0;

            string[] currentParts = currentVersion.Split('.');
            string[] newParts = newVersion.Split('.');

            if (currentParts.Length >= 1) {
                int.TryParse(currentParts[0], out majorCurrent);
                if (currentParts.Length >= 2) {
                    int.TryParse(currentParts[1], out minorCurrent);
                    if (currentParts.Length >= 3) {
                        int.TryParse(currentParts[2], out buildCurrent);
                        if (currentParts.Length >= 4) {
                            int.TryParse(currentParts[3], out revCurrent);
                        }
                    }
                }
            }

            if (newParts.Length >= 1) {
                int.TryParse(newParts[0], out majorNew);
                if (newParts.Length >= 2) {
                    int.TryParse(newParts[1], out minorNew);
                    if (newParts.Length >= 3) {
                        int.TryParse(newParts[2], out buildNew);
                        if (newParts.Length >= 4) {
                            int.TryParse(newParts[3], out revNew);
                        }
                    }
                }
            }

            return (majorNew > majorCurrent ||
                (majorNew == majorCurrent && (minorNew > minorCurrent ||
                    (minorNew == minorCurrent && (buildNew > buildCurrent ||
                        (buildNew == buildCurrent && revNew > revCurrent))))));
        }
    }
}