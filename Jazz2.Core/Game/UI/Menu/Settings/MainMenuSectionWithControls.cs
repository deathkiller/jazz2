using System;
using Duality;
using Duality.Drawing;
using Duality.Input;

namespace Jazz2.Game.UI.Menu.Settings
{
    public class MainMenuSectionWithControls : MainMenuSection
    {
        protected MenuControlBase[] controls;

        private int selectedIndex;
        private float animation;

        public MainMenuSectionWithControls()
        {
        }

        public override void OnShow(MainMenu root)
        {
            animation = 0f;

            base.OnShow(root);
        }

        public override void OnPaint(Canvas canvas)
        {
            if (controls == null) {
                return;
            }

            IDrawDevice device = canvas.DrawDevice;

            Vector2 center = device.TargetSize * 0.5f;
            center.Y *= 0.75f;

            for (int i = 0; i < controls.Length; i++) {
                controls[i].OnDraw(canvas, ref center, selectedIndex == i);
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
                } else if (ControlScheme.MenuActionHit(PlayerActions.Down)) {
                    api.PlaySound("MenuSelect", 0.4f);
                    animation = 0f;
                    if (selectedIndex < controls.Length - 1) {
                        selectedIndex++;
                    } else {
                        selectedIndex = 0;
                    }
                } else if (DualityApp.Keyboard.KeyHit(Key.Escape)) {
                    api.PlaySound("MenuSelect", 0.5f);
                    api.LeaveSection(this);
                }
            }
        }
    }
}