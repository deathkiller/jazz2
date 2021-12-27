#if !SERVER

using System;
using System.IO;
using System.Runtime;
using Duality;
using Duality.Backend;
using Duality.IO;
using Duality.Resources;
using Jazz2.Game.Structs;
using Jazz2.Game.UI;
using Jazz2.Game.UI.Menu;
using Jazz2.Storage;

namespace Jazz2.Game
{
    public partial class App
    {
        private INativeWindow window;
        private bool isInstallationComplete;
#if !DISABLE_CHEATS
        private bool enableCheats;
#endif

        public string Title
        {
            get { return window.Title; }
            set { window.Title = App.AssemblyTitle + (string.IsNullOrEmpty(value) ? "" : " - " + value); }
        }

        public RefreshMode RefreshMode
        {
            get
            {
                return window.RefreshMode;
            }
            set
            {
                window.RefreshMode = value;
            }
        }

        public ScreenMode ScreenMode
        {
            get
            {
                return (window.ScreenMode & ~ScreenMode.Immersive);
            }
            set
            {
                ScreenMode previousValue = (window.ScreenMode & ~ScreenMode.Immersive);
                ScreenMode newValue = (value & ~ScreenMode.Immersive);
                if (previousValue == newValue) {
                    return;
                }

                window.ScreenMode = newValue | (window.ScreenMode & ScreenMode.Immersive);

                if ((value & (ScreenMode.FullWindow | ScreenMode.ChangeResolution)) == 0) {
                    window.Size = LevelRenderSetup.TargetSize;
                }
            }
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

        public bool EnableCheats
        {
            get
            {
#if !DISABLE_CHEATS
                return enableCheats;
#else
                return false;
#endif
            }
            set
            {
#if !DISABLE_CHEATS
                enableCheats = value;
#endif
            }
        }

        public App(INativeWindow window)
        {
            this.window = window;

            this.isInstallationComplete = IsInstallationComplete();

            // Load settings to cache
            SettingsCache.Refresh();

            InitRichPresence();
        }

        public MainMenu ShowMainMenu(bool afterIntro)
        {
#if MULTIPLAYER
            if (client != null) {
                client.OnDisconnected -= OnPacketDisconnected;
                client.Close();
                client = null;
            }
#endif

            if (!this.isInstallationComplete) {
                // If it's incomplete, check it again to be sure
                this.isInstallationComplete = IsInstallationComplete();
            }

            GCSettings.LatencyMode = GCLatencyMode.Interactive;

            ContentResolver.Current.ResetReferenceFlag();

            Scene.Current.DisposeLater();
            MainMenu mainMenu = new MainMenu(this, this.isInstallationComplete, afterIntro);
            Scene.SwitchTo(mainMenu);

            ContentResolver.Current.ReleaseUnreferencedResources();

            UpdateRichPresence(null, null);

            return mainMenu;
        }

        public void PlayCinematics(string name, Action<bool> endCallback)
        {
            GCSettings.LatencyMode = GCLatencyMode.Interactive;

            Scene.Current.DisposeLater();
            Scene.SwitchTo(new Cinematics(this, name, endCallback));

            UpdateRichPresence(null, null);
        }

        public void ChangeLevel(LevelInitialization levelInit)
        {
            ContentResolver.Current.ResetReferenceFlag();

            if (string.IsNullOrEmpty(levelInit.LevelName)) {
                // Next level not specified, so show main menu
                ShowMainMenu(false);
            } else if (levelInit.LevelName == ":end") {
                // End of episode

                if (!string.IsNullOrEmpty(levelInit.LastEpisodeName) && levelInit.LastEpisodeName != "unknown") {
                    // ToDo: Implement time
                    Preferences.Set("EpisodeEnd_Time_" + levelInit.LastEpisodeName, (int)1);

                    if (levelInit.PlayerCarryOvers.Length == 1) {
                        // Save CarryOvers only in SinglePlayer mode
                        ref PlayerCarryOver player = ref levelInit.PlayerCarryOvers[0];

                        Preferences.Set("EpisodeEnd_Lives_" + levelInit.LastEpisodeName, (byte)player.Lives);
                        Preferences.Set("EpisodeEnd_Score_" + levelInit.LastEpisodeName, player.Score);
                        if (player.Ammo != null) {
                            Preferences.Set("EpisodeEnd_Ammo_" + levelInit.LastEpisodeName, player.Ammo);
                        }

                        // Save WeaponUpgrades only if at least one of them is upgraded
                        if (player.WeaponUpgrades != null) {
                            for (int i = 0; i < player.WeaponUpgrades.Length; i++) {
                                if (player.WeaponUpgrades[i] != 0) {
                                    Preferences.Set("EpisodeEnd_Upgrades_" + levelInit.LastEpisodeName, player.WeaponUpgrades);
                                    break;
                                }
                            }
                        }
                    }

                    // Remove existing continue data
                    Preferences.Remove("EpisodeContinue_Misc_" + levelInit.LastEpisodeName);
                    Preferences.Remove("EpisodeContinue_Score_" + levelInit.LastEpisodeName);
                    Preferences.Remove("EpisodeContinue_Level_" + levelInit.LastEpisodeName);
                    Preferences.Remove("EpisodeContinue_Ammo_" + levelInit.LastEpisodeName);
                    Preferences.Remove("EpisodeContinue_Upgrades_" + levelInit.LastEpisodeName);

                    Preferences.Commit();

                    Episode lastEpisode = GetEpisode(levelInit.LastEpisodeName);
                    if (lastEpisode != null && !string.IsNullOrEmpty(lastEpisode.NextEpisode)) {
                        // Redirect to next episode
                        Episode nextEpisode = GetEpisode(lastEpisode.NextEpisode);

                        levelInit.EpisodeName = lastEpisode.NextEpisode;
                        levelInit.LevelName = nextEpisode.FirstLevel;

                        // Start new level
                        LevelHandler handler = new LevelHandler(this, levelInit);

                        Scene.Current.DisposeLater();
                        Scene.SwitchTo(handler);

                        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                        GC.Collect();
                        GC.WaitForPendingFinalizers();

                        GCSettings.LatencyMode = GCLatencyMode.LowLatency;

                        UpdateRichPresence(levelInit, null);
                    } else {
                        // Next episode not found...
                        ShowMainMenu(false);
                    }
                } else {
                    // Shouldn't happen...
                    ShowMainMenu(false);
                }
            } else if (levelInit.LevelName == ":credits") {
                // End of game

                if (!string.IsNullOrEmpty(levelInit.LastEpisodeName) && levelInit.LastEpisodeName != "unknown" && levelInit.PlayerCarryOvers.Length == 1) {
                    // ToDo: Implement time
                    Preferences.Set("EpisodeEnd_Time_" + levelInit.LastEpisodeName, (int)1);

                    // Remove existing continue data
                    Preferences.Remove("EpisodeContinue_Misc_" + levelInit.LastEpisodeName);
                    Preferences.Remove("EpisodeContinue_Score_" + levelInit.LastEpisodeName);
                    Preferences.Remove("EpisodeContinue_Level_" + levelInit.LastEpisodeName);
                    Preferences.Remove("EpisodeContinue_Ammo_" + levelInit.LastEpisodeName);
                    Preferences.Remove("EpisodeContinue_Upgrades_" + levelInit.LastEpisodeName);

                    Preferences.Commit();
                }

                // ToDo: Show credits

                PlayCinematics("ending", endOfStream => {
                    ShowMainMenu(false).SwitchToSection(new AboutSection());
                });
            } else {
                if (!string.IsNullOrEmpty(levelInit.EpisodeName) && levelInit.EpisodeName != "unknown") {
                    Episode nextEpisode = GetEpisode(levelInit.EpisodeName);
                    if (nextEpisode.FirstLevel != levelInit.LevelName) {
                        // Save continue data, if it's not the first level of the episode
                        ref PlayerCarryOver player = ref levelInit.PlayerCarryOvers[0];

                        Preferences.Set("EpisodeContinue_Misc_" + levelInit.EpisodeName, new[] { (byte)player.Lives, (byte)levelInit.Difficulty, (byte)player.Type });
                        Preferences.Set("EpisodeContinue_Score_" + levelInit.EpisodeName, player.Score);
                        Preferences.Set("EpisodeContinue_Level_" + levelInit.EpisodeName, levelInit.LevelName);
                        if (player.Ammo != null) {
                            Preferences.Set("EpisodeContinue_Ammo_" + levelInit.EpisodeName, player.Ammo);
                        }

                        // Save WeaponUpgrades only if at least one of them is upgraded
                        if (player.WeaponUpgrades != null) {
                            for (int i = 0; i < player.WeaponUpgrades.Length; i++) {
                                if (player.WeaponUpgrades[i] != 0) {
                                    Preferences.Set("EpisodeContinue_Upgrades_" + levelInit.EpisodeName, player.WeaponUpgrades);
                                    break;
                                }
                            }
                        }

                        Preferences.Commit();
                    }
                }

                try {
                    LevelHandler handler = new LevelHandler(this, levelInit);

                    Scene.Current.DisposeLater();
                    Scene.SwitchTo(handler);

                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    GCSettings.LatencyMode = GCLatencyMode.LowLatency;

                    UpdateRichPresence(levelInit, null);
                } catch (Exception ex) {
                    Log.Write(LogType.Error, "Cannot load level: " + ex);

                    // TODO: translation
                    ShowMainMenu(false).SwitchToSection(new SimpleMessageSection("error/unknown".T(), ex.Message));
                }
            }

            ContentResolver.Current.ReleaseUnreferencedResources();
        }

        private static Episode GetEpisode(string name)
        {
            string pathAbsolute = PathOp.Combine(DualityApp.DataDirectory, "Episodes", name, "Episode.res");
            if (FileOp.Exists(pathAbsolute)) {
                Episode json;
                using (Stream s = FileOp.Open(pathAbsolute, FileAccessMode.Read)) {
                    json = ContentResolver.Current.ParseJson<Episode>(s);
                }

                return json;
            } else {
                return null;
            }
        }

        private bool IsInstallationComplete()
        {
#if UNCOMPRESSED_CONTENT || PLATFORM_WASM
            return true;
#else
            return DirectoryOp.Exists(PathOp.Combine(DualityApp.DataDirectory, "Episodes")) &&
                DirectoryOp.Exists(PathOp.Combine(DualityApp.DataDirectory, "i18n")) &&
                //DirectoryOp.Exists(PathOp.Combine(DualityApp.DataDirectory, "Music")) &&
                DirectoryOp.Exists(PathOp.Combine(DualityApp.DataDirectory, "Tilesets")) &&
                FileOp.Exists(PathOp.Combine(DualityApp.DataDirectory, "Main.dz", "Animations", "Jazz", "idle.png")) &&
                FileOp.Exists(PathOp.Combine(DualityApp.DataDirectory, "Main.dz", "Metadata", "UI", "MainMenu.res"));
#endif
        }

        partial void InitRichPresence();
        partial void UpdateRichPresence(LevelInitialization? levelInit, string targetName);
    }
}

#endif