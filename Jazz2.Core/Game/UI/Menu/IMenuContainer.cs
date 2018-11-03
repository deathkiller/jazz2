using System;
using Duality;
using Duality.Drawing;
using Jazz2.Game.Structs;

namespace Jazz2.Game.UI.Menu
{
    public interface IMenuContainer
    {
        ScreenMode ScreenMode { get; set; }

        void SwitchToSection(MenuSection section);

        void LeaveSection(MenuSection section);

        void BeginFadeOut(Action action);

        void SwitchToLevel(LevelInitialization data);

#if MULTIPLAYER
        void SwitchToServer(System.Net.IPEndPoint endPoint);
#endif

        void DrawString(ref int charOffset, string text, float x,
            float y, Alignment align, ColorRgba? color, float scale = 1f, float angleOffset = 0f, float varianceX = 4f, float varianceY = 4f,
            float speed = 4f, float charSpacing = 1f, float lineSpacing = 1f);

        void DrawStringShadow(ref int charOffset, string text, float x,
            float y, Alignment align, ColorRgba? color, float scale = 1f, float angleOffset = 0f, float varianceX = 4f, float varianceY = 4f,
            float speed = 4f, float charSpacing = 1f, float lineSpacing = 1f);

        void DrawMaterial(string name, float x, float y, Alignment alignment, ColorRgba color, float scaleX = 1f, float scaleY = 1f);

        void DrawMaterial(string name, float x, float y, Alignment alignment, ColorRgba color, float scaleX, float scaleY, Rect texRect);

        void DrawMaterial(string name, int frame, float x, float y, Alignment alignment, ColorRgba color, float scaleX = 1f, float scaleY = 1f);

        void PlaySound(string name, float volume = 1f);

        bool IsAnimationPresent(string name);
    }
}