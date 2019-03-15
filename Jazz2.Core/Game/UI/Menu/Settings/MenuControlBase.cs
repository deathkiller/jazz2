using Duality;
using Duality.Drawing;

namespace Jazz2.Game.UI.Menu.Settings
{
    public abstract class MenuControlBase
    {
        protected readonly IMenuContainer api;

        public abstract bool IsEnabled { get; set; }
        public abstract bool IsInputCaptured { get; }

        public MenuControlBase(IMenuContainer api)
        {
            this.api = api;
        }

        public abstract void OnDraw(Canvas canvas, ref Vector2 pos, bool focused, float animation);

        public abstract void OnUpdate();
    }
}