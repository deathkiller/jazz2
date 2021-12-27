﻿using Duality.Backend.DefaultOpenTK;
using Duality.Drawing;
using Jazz2;
using Jazz2.Game;
using OpenTK;
using OpenTK.Graphics;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace Duality.Backend.Es20
{
    public class NativeWindow : INativeWindow
    {
        private class InternalWindow : GameWindow
        {
            private NativeWindow parent;

            public InternalWindow(NativeWindow parent, int w, int h, GraphicsMode mode, string title,
                GameWindowFlags flags, GraphicsContextFlags contextFlags)
                : base(w, h, mode, title, flags, DisplayDevice.Default, 2, 0, contextFlags)
            {
                this.parent = parent;
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                this.parent.OnResize(e);
            }

            protected override void OnUpdateFrame(FrameEventArgs e)
            {
                base.OnUpdateFrame(e);
                this.parent.OnUpdateFrame(e);
            }

            protected override void OnRenderFrame(FrameEventArgs e)
            {
                base.OnRenderFrame(e);
                this.parent.OnRenderFrame(e);
            }
        }

        private InternalWindow internalWindow;
        private RefreshMode refreshMode;
        private Stopwatch frameLimiterWatch = new Stopwatch();
        private ScreenMode screenMode;

        public int Width
        {
            get { return this.internalWindow.ClientSize.Width; }
        }

        public int Height
        {
            get { return this.internalWindow.ClientSize.Height; }
        }

        public Point2 Size
        {
            get { return new Point2(this.Width, this.Height); }
            set
            {
                this.internalWindow.ClientSize = new Size(value.X, value.Y);

                Point location = this.internalWindow.Location;
                if (location.X < 50) location.X = 50;
                if (location.Y < 50) location.Y = 50;
                this.internalWindow.Location = location;

                DualityApp.WindowSize = new Point2(this.internalWindow.ClientSize.Width, this.internalWindow.ClientSize.Height);
            }
        }

        public bool IsMultisampled
        {
            get { return this.internalWindow.Context.GraphicsMode.Samples > 0; }
        }

        public string Title
        {
            get { return this.internalWindow.Title; }
            set { this.internalWindow.Title = value; }
        }

        public RefreshMode RefreshMode
        {
            get { return refreshMode; }
            set
            {
                if (refreshMode == value) {
                    return;
                }

                VSyncMode vsyncMode;
                switch (value) {
                    default:
                    case RefreshMode.NoSync:
                    case RefreshMode.ManualSync:
                        vsyncMode = VSyncMode.Off;
                        break;
                    case RefreshMode.VSync:
                        vsyncMode = VSyncMode.On;
                        break;
                    case RefreshMode.AdaptiveVSync:
                        vsyncMode = VSyncMode.Adaptive;
                        break;
                }

                this.internalWindow.VSync = vsyncMode;

                refreshMode = value;
            }
        }

        public ScreenMode ScreenMode
        {
            get { return screenMode; }
            set
            {
                value &= (ScreenMode.FullWindow | ScreenMode.FixedSize | ScreenMode.ChangeResolution);
                if (screenMode == value) {
                    return;
                }

                if ((value & (ScreenMode.FullWindow | ScreenMode.ChangeResolution)) != 0) {
                    this.internalWindow.WindowState = WindowState.Fullscreen;
                    this.internalWindow.WindowBorder = WindowBorder.Hidden;
                } else {
                    this.internalWindow.WindowState = WindowState.Normal;
                    if ((value & ScreenMode.FixedSize) != 0)
                        this.internalWindow.WindowBorder = WindowBorder.Fixed;
                    else
                        this.internalWindow.WindowBorder = WindowBorder.Resizable;
                }

                if ((value & ScreenMode.FullWindow) != 0) {
                    this.internalWindow.Cursor = MouseCursor.Empty;
                } else {
                    this.internalWindow.Cursor = MouseCursor.Default;
                }

                screenMode = value;
            }
        }

        public NativeWindow(GraphicsMode mode, WindowOptions options)
        {
            if ((options.ScreenMode & (ScreenMode.ChangeResolution | ScreenMode.FullWindow)) != 0) {
                if (DisplayDevice.Default != null) {
                    options.Size = new Point2(
                        DisplayDevice.Default.Width,
                        DisplayDevice.Default.Height);
                }
            }

            screenMode = options.ScreenMode & (ScreenMode.FullWindow | ScreenMode.FixedSize | ScreenMode.ChangeResolution);

            GameWindowFlags windowFlags = GameWindowFlags.Default;
            if ((screenMode & ScreenMode.FixedSize) != 0)
                windowFlags = GameWindowFlags.FixedWindow;
            else if ((screenMode & ScreenMode.FullWindow) != 0)
                windowFlags = GameWindowFlags.Fullscreen;

            VSyncMode vsyncMode;
            switch (options.RefreshMode) {
                default:
                case RefreshMode.NoSync:
                case RefreshMode.ManualSync:
                    vsyncMode = VSyncMode.Off;
                    break;
                case RefreshMode.VSync:
                    vsyncMode = VSyncMode.On;
                    break;
                case RefreshMode.AdaptiveVSync:
                    vsyncMode = VSyncMode.Adaptive;
                    break;
            }

            this.refreshMode = options.RefreshMode;
            try {
                this.internalWindow = new InternalWindow(
                    this,
                    options.Size.X,
                    options.Size.Y,
                    mode,
                    options.Title,
                    windowFlags,
                    GraphicsContextFlags.Embedded);
            } catch {
                this.internalWindow = new InternalWindow(
                    this,
                    options.Size.X,
                    options.Size.Y,
                    mode,
                    options.Title,
                    windowFlags,
                    GraphicsContextFlags.Default);
            }
            this.internalWindow.MakeCurrent();

            this.internalWindow.CursorVisible = true;
            if ((screenMode & ScreenMode.FullWindow) != 0)
                this.internalWindow.Cursor = MouseCursor.Empty;

            this.internalWindow.VSync = vsyncMode;


            GraphicsBackend.LogOpenGLSpecs();

            Log.Write(LogType.Info,
                "Window Specification: " + Environment.NewLine +
                "  Buffers: {0}" + Environment.NewLine +
                "  Samples: {1}" + Environment.NewLine +
                "  ColorFormat: {2}" + Environment.NewLine +
                "  AccumFormat: {3}" + Environment.NewLine +
                "  Depth: {4}" + Environment.NewLine +
                "  Stencil: {5}" + Environment.NewLine +
                "  VSync: {6}" + Environment.NewLine +
                "  SwapInterval: {7}",
                this.internalWindow.Context.GraphicsMode.Buffers,
                this.internalWindow.Context.GraphicsMode.Samples,
                this.internalWindow.Context.GraphicsMode.ColorFormat,
                this.internalWindow.Context.GraphicsMode.AccumulatorFormat,
                this.internalWindow.Context.GraphicsMode.Depth,
                this.internalWindow.Context.GraphicsMode.Stencil,
                this.internalWindow.VSync,
                this.internalWindow.Context.SwapInterval);

            // Retrieve icon from executable file and set it as window icon
            string executablePath = null;
            try {
                Assembly entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    executablePath = Path.GetFullPath(entryAssembly.Location);
                    if (File.Exists(executablePath))
                    {
                        this.internalWindow.Icon = Icon.ExtractAssociatedIcon(executablePath);
                    }
                }
            }
            // As described in issue 301 (https://github.com/AdamsLair/duality/issues/301), the
            // icon extraction can fail with an exception under certain circumstances. Don't fail
            // just because of an icon. Log the error and continue.
            catch (Exception e) {
                Log.Write(LogType.Warning,
                    "There was an exception while trying to extract the " +
                    "window icon from the game's main executable '{0}'. This is " +
                    "uncritical, but still an error: {1}",
                    executablePath,
                    e);
            }

            if ((screenMode & ScreenMode.FullWindow) != 0)
                this.internalWindow.WindowState = WindowState.Fullscreen;

            DualityApp.WindowSize = new Point2(this.internalWindow.ClientSize.Width,
                this.internalWindow.ClientSize.Height);

            // Register events and input
            this.HookIntoDuality();
        }

        void INativeWindow.Run()
        {
            this.internalWindow.Run();
        }

        void IDisposable.Dispose()
        {
            this.UnhookFromDuality();
            if (this.internalWindow != null) {
                this.internalWindow.Dispose();
                this.internalWindow = null;
            }
        }

        internal void HookIntoDuality()
        {
            DualityApp.Mouse.Source = new GameWindowMouseInputSource(this.internalWindow);
            DualityApp.Keyboard.Source = new GameWindowKeyboardInputSource(this.internalWindow);
        }

        internal void UnhookFromDuality()
        {
            if (DualityApp.Mouse.Source is GameWindowMouseInputSource)
                DualityApp.Mouse.Source = null;
            if (DualityApp.Keyboard.Source is GameWindowKeyboardInputSource)
                DualityApp.Keyboard.Source = null;
        }

        private void OnResize(EventArgs e)
        {
            DualityApp.WindowSize = this.Size;
            DrawDevice.RenderVoid(new Rect(this.Size));
        }

        private void OnUpdateFrame(FrameEventArgs e)
        {
            if (DualityApp.ExecContext == DualityApp.ExecutionContext.Terminated) {
                this.internalWindow.Close();
                return;
            }

            // Give the processor a rest if we have the time, don't use 100% CPU even without VSync
            if (this.frameLimiterWatch.IsRunning && this.refreshMode == RefreshMode.ManualSync) {
                while (this.frameLimiterWatch.ElapsedMilliseconds < Time.MillisecondsPerFrame) {
                    // Enough leftover time? Risk a short sleep, don't burn CPU waiting.
                    if (this.frameLimiterWatch.ElapsedMilliseconds < Time.MillisecondsPerFrame * 0.75f)
                        System.Threading.Thread.Sleep(0);
                }
            }
            this.frameLimiterWatch.Restart();
            DualityApp.Update();
        }

        private void OnRenderFrame(FrameEventArgs e)
        {
            if (DualityApp.ExecContext == DualityApp.ExecutionContext.Terminated)
                return;

            Vector2 imageSize;
            Rect viewportRect;
            DualityApp.CalculateGameViewport(this.Size, out viewportRect, out imageSize);

            DualityApp.Render(null, viewportRect, imageSize);

            this.internalWindow.SwapBuffers();
        }
    }
}