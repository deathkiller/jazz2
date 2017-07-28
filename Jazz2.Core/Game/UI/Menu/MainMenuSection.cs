using Duality.Drawing;

namespace Jazz2.Game.UI.Menu
{
    public abstract class MainMenuSection
    {
        protected MainMenu api;

        public virtual void OnShow(MainMenu api)
        {
            this.api = api;
        }

        public virtual void OnHide()
        {
            this.api = null;
        }

        public abstract void OnUpdate();

        public abstract void OnPaint(IDrawDevice device, Canvas c);
    }
}