using System;
using Duality;
using Duality.Drawing;

namespace Jazz2.Game.UI.Menu.Settings
{
    public class MenuSectionWithControls : MenuSection
    {
        protected MenuControlBase[] controls;
        protected int selectedIndex;

        private int scrollOffset;
        private int maxVisibleItems = 5;
        private float animation;

        public override void OnShow(IMenuContainer root)
        {
            animation = 0f;

            base.OnShow(root);
        }

        public override void OnPaint(Canvas canvas, Rect view)
        {
            if (controls == null) {
                return;
            }

            IDrawDevice device = canvas.DrawDevice;

            Vector2 size = device.TargetSize;

            Vector2 pos = size * 0.5f;
            pos.Y *= 0.65f;

            float maxVisibleItemsFloat = (size.Y - pos.Y - 20f) / 45f;
            maxVisibleItems = (int)maxVisibleItemsFloat;

            if (maxVisibleItems > controls.Length) {
                maxVisibleItems = controls.Length;
            }

            pos.Y += (maxVisibleItemsFloat - maxVisibleItems) / maxVisibleItems * 45f;

            for (int i = 0; i < maxVisibleItems; i++) {
                int idx = i + scrollOffset;
                if (idx >= controls.Length) {
                    break;
                }

                controls[idx].OnDraw(canvas, ref pos, selectedIndex == idx, animation);
            }
        }

        public override void OnUpdate()
        {
            if (controls == null) {
                return;
            }

            if (animation < 1f) {
                animation = Math.Min(animation + Time.TimeMult * 0.016f, 1f);
            }

            controls[selectedIndex].OnUpdate();

            if (!controls[selectedIndex].IsInputCaptured) {
                if (ControlScheme.MenuActionHit(PlayerActions.Fire)) {
                    //
                } else if (ControlScheme.MenuActionHit(PlayerActions.Up)) {
                    api.PlaySound("MenuSelect", 0.4f);
                    animation = 0f;
                    if (selectedIndex > 0) {
                        selectedIndex--;
                    } else {
                        selectedIndex = controls.Length - 1;
                    }

                    int requiredOffset;
                    if (scrollOffset > (requiredOffset = Math.Max(0, selectedIndex - 1))) {
                        scrollOffset = requiredOffset;
                    } else if (scrollOffset < (requiredOffset = Math.Min(controls.Length - maxVisibleItems, selectedIndex - maxVisibleItems + 2))) {
                        scrollOffset = requiredOffset;
                    }
                } else if (ControlScheme.MenuActionHit(PlayerActions.Down)) {
                    api.PlaySound("MenuSelect", 0.4f);
                    animation = 0f;
                    if (selectedIndex < controls.Length - 1) {
                        selectedIndex++;
                    } else {
                        selectedIndex = 0;
                    }

                    int requiredOffset;
                    if (scrollOffset > (requiredOffset = Math.Max(0, selectedIndex - 1))) {
                        scrollOffset = requiredOffset;
                    } else if (scrollOffset < (requiredOffset = Math.Min(controls.Length - maxVisibleItems, selectedIndex - maxVisibleItems + 2))) {
                        scrollOffset = requiredOffset;
                    }
                } else if (ControlScheme.MenuActionHit(PlayerActions.Menu)) {
                    api.PlaySound("MenuSelect", 0.5f);
                    api.LeaveSection(this);
                }
            }
        }
    }
}