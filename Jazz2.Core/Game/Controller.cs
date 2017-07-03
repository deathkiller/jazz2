using Duality;
using Duality.Backend;
using Duality.Resources;
using Jazz2.Game.Menu;
using Jazz2.Game.Structs;

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
            ContentResolver.Current.ResetReferenceFlag();

            Scene.Current.DisposeLater();
            Scene.SwitchTo(new MainMenu(this));

            ContentResolver.Current.ReleaseUnreferencedResources();
        }

        public void ChangeLevel(InitLevelData carryOver = default(InitLevelData))
        {
            ContentResolver.Current.ResetReferenceFlag();

            if (string.IsNullOrEmpty(carryOver.LevelName) || carryOver.LevelName == "endepis" || carryOver.LevelName == "ending") {
                // End of the episode, etc. - Return to menu
                ShowMainMenu();
            } else {
                LevelHandler levelManager = new LevelHandler(this, carryOver);

                Scene.Current.DisposeLater();
                Scene.SwitchTo(levelManager);
            }

            ContentResolver.Current.ReleaseUnreferencedResources();
        }
    }
}