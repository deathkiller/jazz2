using System;

namespace Duality
{
    /// <summary>
    /// Describes the way a Duality window is set up.
    /// </summary>
    [Flags]
    public enum ScreenMode
    {
        Window = 0,

        FixedSize = 1 << 0,
        FullWindow = 1 << 1,
        ChangeResolution = 1 << 2,

        Immersive = 1 << 3,
    }
}