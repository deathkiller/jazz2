using System;

namespace Duality.Backend
{
    public interface INativeWindow : IDisposable
	{
        void Run();

        string Title { get; set; }

        Point2 Size { get; set; }

        RefreshMode RefreshMode { get; set; }

        ScreenMode ScreenMode { get; set; }
    }
}
