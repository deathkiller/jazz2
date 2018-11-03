using Duality.Drawing;

namespace Jazz2.Game.UI.Menu
{
    public abstract class MenuSection
    {
        protected IMenuContainer api;

        public virtual void OnShow(IMenuContainer api)
        {
            this.api = api;
        }

        public virtual void OnHide(bool isRemoved)
        {
            this.api = null;
        }

        public abstract void OnUpdate();

        public abstract void OnPaint(Canvas canvas);
    }
}