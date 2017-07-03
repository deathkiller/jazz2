using System;

namespace Duality.Backend.Dummy
{
    internal class DummyNativeWindow : INativeWindow
	{
	    void IDisposable.Dispose() { }

		void INativeWindow.Run()
		{
			while (DualityApp.ExecContext != DualityApp.ExecutionContext.Terminated)
			{
				DualityApp.Update();
                DualityApp.Render(null, new Rect(640, 480), new Vector2(640, 480));
            }
		}

        string INativeWindow.Title
        {
            get { return null; }
            set { }
        }

	    public ScreenMode ScreenMode
	    {
	        get { return ScreenMode.Window; }
	        set { }
	    }
	}
}
