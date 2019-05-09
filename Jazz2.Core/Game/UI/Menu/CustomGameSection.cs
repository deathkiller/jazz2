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
                new LinkControl(api, "menu/play custom/single".T(), OnSingleplayerPressed, "menu/play custom/single/desc".T()),
#if ENABLE_SPLITSCREEN
                new LinkControl(api, "menu/play custom/split".T(), OnSplitscreenPressed, "menu/play custom/split/desc".T()),
#endif
                new LinkControl(api, "menu/play custom/multi".T(), OnOnlineMultiplayerPressed, "menu/play custom/multi/desc".T())
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