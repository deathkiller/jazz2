using Duality.Drawing;

namespace Jazz2.Game.UI.Menu.I
{
    public abstract class InGameMenuSection
    {
        protected InGameMenu api;

        public virtual void OnShow(InGameMenu api)
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