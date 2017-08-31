using System.IO;
using System.Text.Json;
using Duality;
using Duality.Backend;
using Duality.IO;
using Duality.Resources;
using Jazz2.Game.Structs;
using Jazz2.Game.UI.Menu;
using Jazz2.Storage;

namespace Jazz2.Game
{
    // ToDo: Remove this controller, move this to "App" class?
    public class Controller
    {
        private readonly INativeWindow window;

        public string Title
        {
            get { return window.Title; }
            set { window.Title = App.AssemblyTitle + (string.IsNullOrEmpty(value) ? "" : " - " + value); }
        }

        public bool Immersive
        {
            get
            {
                return (window.ScreenMode & ScreenMode.Immersive) != 0;
            }
            set
            {
                if (value) {
                    window.ScreenMode |= ScreenMode.Immersive;
                } else {
                    window.ScreenMode &= ~ScreenMode.Immersive;
                }
            }
        }

        public Controller(INativeWindow window)
        {
            this.window = window;
        }

        public void ShowMainMenu()
        {
#if MULTIPLAYER
            if (network != null) {
                network.Close();
                network = null;
            }
#endif

            ContentResolver.Current.ResetReferenceFlag();

            Scene.Current.DisposeLater();
            Scene.SwitchTo(new MainMenu(this));

            ContentResolver.Current.ReleaseUnreferencedResources();
        }

        public void ChangeLevel(LevelInitialization carryOver = default(LevelInitialization))
        {
            ContentResolver.Current.ResetReferenceFlag();

            if (string.IsNullOrEmpty(carryOver.LevelName)) {
                // Next level not specified, so show main menu
                ShowMainMenu();
            } else if (carryOver.LevelName == ":end") {
                // End of episode

                if (!string.IsNullOrEmpty(carryOver.LastEpisodeName) && carryOver.LastEpisodeName != "unknown") {
                    // ToDo: Implement time
                    Preferences.Set("EpisodeTime_" + carryOver.LastEpisodeName, (int)1);

                    if (carryOver.PlayerCarryOvers.Length == 1) {
                        // Save CarryOvers only in SinglePlayer mode
                        ref PlayerCarryOver player = ref carryOver.PlayerCarryOvers[0];

                        Preferences.Set("EpisodeLives_" + carryOver.LastEpisodeName, (byte)player.Lives);
                        Preferences.Set("EpisodeAmmo_" + carryOver.LastEpisodeName, player.Ammo);

                        // Save WeaponUpgrades only if at least one of them is upgraded
                        for (int i = 0; i < player.WeaponUpgrades.Length; i++) {
                            if (player.WeaponUpgrades[i] != 0) {
                                Preferences.Set("EpisodeUpgrades_" + carryOver.LastEpisodeName, player.WeaponUpgrades);
                                break;
                            }
                        }
                    }

                    Preferences.Commit();

                    Episode lastEpisode = GetEpisode(carryOver.LastEpisodeName);
                    if (lastEpisode != null && !string.IsNullOrEmpty(lastEpisode.NextEpisode)) {
                        // Redirect to next episode
                        Episode nextEpisode = GetEpisode(lastEpisode.NextEpisode);

                        carryOver.EpisodeName = lastEpisode.NextEpisode;
                        carryOver.LevelName = nextEpisode.FirstLevel;

                        // Start new level
                        LevelHandler handler = new LevelHandler(this, carryOver);

                        Scene.Current.DisposeLater();
                        Scene.SwitchTo(handler);
                    } else {
                        // Next episode not found...
                        ShowMainMenu();
                    }
                } else {
                    // Shouldn't happen...
                    ShowMainMenu();
                }
            } else if (carryOver.LevelName == ":credits") {
                // End of game

                if (!string.IsNullOrEmpty(carryOver.LastEpisodeName) && carryOver.LastEpisodeName != "unknown") {
                    // ToDo: Implement time
                    Preferences.Set("EpisodeTime_" + carryOver.LastEpisodeName, (int)1);

                    Preferences.Commit();
                }

                // ToDo: Show credits
                ShowMainMenu();
            } else {
                LevelHandler handler = new LevelHandler(this, carryOver);

                Scene.Current.DisposeLater();
                Scene.SwitchTo(handler);
            }

            ContentResolver.Current.ReleaseUnreferencedResources();
        }

#if MULTIPLAYER
        private Multiplayer.NetworkHandler network;

        public void ConnectToServer(System.Net.IPEndPoint endPoint)
        {
            // ToDo
            const string token = "J²";

            if (network != null) {
                network.Close();
            }

            network = new Multiplayer.NetworkHandler(token);
            network.RegisterCallback<NetworkPackets.Server.LoadLevel>(OnLoadLevel);
            network.Connect(endPoint);
        }

        private void OnLoadLevel(ref NetworkPackets.Server.LoadLevel p)
        {
            string levelName = p.LevelName;
            int playerIndex = p.AssignedPlayerIndex;

            DispatchToMainThread(delegate {
                Multiplayer.NetworkLevelHandler handler = new Multiplayer.NetworkLevelHandler(this, network,
                    new LevelInitialization("unknown", levelName, GameDifficulty.Default, Actors.PlayerType.Jazz),
                    playerIndex);

                Scene.Current.DisposeLater();
                Scene.SwitchTo(handler);

                network.Send(new NetworkPackets.Client.LevelReady {
                    Index = playerIndex
                }, 2, Lidgren.Network.NetDeliveryMethod.ReliableSequenced, NetworkPackets.NetworkChannels.Main);
            });
        }


        public void DispatchToMainThread(System.Action action)
        {
            DualityApp.DisposeLater(new ActionDisposable(action));
        }

        private class ActionDisposable : System.IDisposable
        {
            private System.Action action;

            public ActionDisposable(System.Action action)
            {
                this.action = action;
            }

            void System.IDisposable.Dispose()
            {
                System.Threading.Interlocked.Exchange(ref action, null)();
            }
        }
#endif

        private static Episode GetEpisode(string name)
        {
            JsonParser jsonParser = new JsonParser();

            string pathAbsolute = PathOp.Combine(DualityApp.DataDirectory, "Episodes", name, ".res");
            if (FileOp.Exists(pathAbsolute)) {
                Episode json;
                using (Stream s = DualityApp.SystemBackend.FileSystem.OpenFile(pathAbsolute, FileAccessMode.Read)) {
                    json = jsonParser.Parse<Episode>(s);
                }

                return json;
            } else {
                return null;
            }
        }
    }
}