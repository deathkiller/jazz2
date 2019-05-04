#if MULTIPLAYER

using Jazz2.Game.UI.Menu.Settings;

namespace Jazz2.Game.UI.Menu
{
    public class CustomGameSection : MenuSectionWithControls
    {
        public override void OnShow(IMenuContainer root)
        {
            base.OnShow(root);

            controls = new MenuControlBase[] {
                new LinkControl(api, "Singleplayer", OnSingleplayerPressed, "Play any custom level alone"),
#if ENABLE_SPLITSCREEN
                new LinkControl(api, "Splitscreen", OnSplitscreenPressed, "Play with up to 3 other players on this PC"),
#endif
                new LinkControl(api, "Online Multiplayer", OnOnlineMultiplayerPressed, "Connect to server and play with other players")
            };
        }

        private void OnSingleplayerPressed()
        {
            api.SwitchToSection(new CustomLevelSelectSection());
        }

#if ENABLE_SPLITSCREEN
        private void OnSplitscreenPressed()
        {
            // ToDo
        }
#endif

        private void OnOnlineMultiplayerPressed()
        {
            api.SwitchToSection(new MultiplayerServerSelectSection());
        }
    }
}

#endif