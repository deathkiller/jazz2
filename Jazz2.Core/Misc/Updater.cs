using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Jazz2
{
    public class Updater
    {
        public delegate void CheckUpdatesCallback(bool newAvailable, Release release);

        public class Release
        {
            public string tag_name { get; set; }
            public string name { get; set; }
            //public string published_at { get; set; }
            //public string body { get; set; }
        }

        private const string Url = "https://api.github.com/repos/deathkiller/jazz2/releases/latest";

        public static void CheckUpdates(CheckUpdatesCallback callback)
        {
            if (callback == null) {
                return;
            }

            ThreadPool.UnsafeQueueUserWorkItem(delegate {
                try {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    WebClient client = new WebClient();
                    client.Encoding = Encoding.UTF8;
                    client.Headers["User-Agent"] = App.AssemblyTitle;

                    string content = client.DownloadString(Url);
                    if (content == null) {
                        callback(false, null);
                        return;
                    }

                    Release release = new JsonParser().Parse<Release>(content);
                    if (release == null || release.tag_name == null) {
                        callback(false, null);
                        return;
                    }

                    bool isNewer = IsVersionNewer(App.AssemblyVersion, release.tag_name);
                    callback(isNewer, release);
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

            string[] currentParts = currentVersion.Split('.');
            string[] newParts = newVersion.Split('.');

            if (currentParts.Length >= 1) {
                int.TryParse(currentParts[0], out majorCurrent);
                if (currentParts.Length >= 2) {
                    int.TryParse(currentParts[1], out minorCurrent);
                    if (currentParts.Length >= 3) {
                        int.TryParse(currentParts[2], out buildCurrent);
                    }
                }
            }

            if (newParts.Length >= 1) {
                int.TryParse(newParts[0], out majorNew);
                if (newParts.Length >= 2) {
                    int.TryParse(newParts[1], out minorNew);
                    if (newParts.Length >= 3) {
                        int.TryParse(newParts[2], out buildNew);
                    }
                }
            }

            return (majorNew > majorCurrent || (majorNew == majorCurrent && (minorNew > minorCurrent || (minorNew == minorCurrent && buildNew > buildCurrent))));
        }
    }
}