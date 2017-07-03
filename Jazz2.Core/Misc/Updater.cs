using System;
using System.Net;
using System.Text;

namespace Jazz2
{
    public class Updater
    {
        public delegate void CheckUpdatesCallback(bool newAvailable, string version);

        public static void CheckUpdates(CheckUpdatesCallback callback)
        {
            if (callback == null) {
                return;
            }

            try {
                WebClient client = new WebClient();
                client.Encoding = Encoding.UTF8;
                client.DownloadStringCompleted += OnCheckUpdatesCompleted;
                client.DownloadStringAsync(new Uri("http://deat.tk/downloads/other/jazz2/updates"), callback);
            } catch {
                // Nothing to do...
                callback(false, null);
            }
        }

        private static void OnCheckUpdatesCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            CheckUpdatesCallback callback = e.UserState as CheckUpdatesCallback;
            if (callback == null) {
                return;
            }
            
            if (e.Error != null || e.Cancelled || e.Result == null || !e.Result.StartsWith("Death™ Updates :: ", StringComparison.InvariantCulture)) {
                callback(false, null);
                return;
            }

            int i = e.Result.IndexOf(' ', 19);
            if (i == -1) {
                callback(false, null);
                return;
            }

            string version = e.Result.Substring(18, i - 18);
            bool isNewer = IsVersionNewer(App.AssemblyVersion, version);
            callback(isNewer, version);
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