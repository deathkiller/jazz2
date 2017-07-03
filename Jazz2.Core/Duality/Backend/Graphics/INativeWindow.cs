using System;

namespace Duality.Backend
{
    public interface INativeWindow : IDisposable
	{
        void Run();

        string Title { get; set; }

        ScreenMode ScreenMode { get; set; }
    }
}
