#if MULTIPLAYER

using Jazz2.Game.UI.Menu.S;

namespace Jazz2.Game.UI.Menu
{
    public class CustomGameSection : MainMenuSectionWithControls
    {
        public override void OnShow(MainMenu root)
        {
            base.OnShow(root);

            controls = new MenuControlBase[] {
                new LinkControl(api, "Single Player", OnSinglePlayerPressed),
                new LinkControl(api, "Party Mode [Alpha]", OnPartyModePressed)
            };
        }

        private void OnSinglePlayerPressed()
        {
            api.SwitchToSection(new CustomLevelSelectSection());
        }

        private void OnPartyModePressed()
        {
            api.SwitchToSection(new MultiplayerServerSelectSection());
        }
    }
}

#endif