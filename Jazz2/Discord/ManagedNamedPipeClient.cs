using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace Jazz2.Discord
{
    internal class ManagedNamedPipeClient
    {
        private const string PipeName = "discord-ipc-{0}";

        private int connectedPipe;
        private NamedPipeClientStream stream;
        private object streamLock = new object();

        private byte[] buffer = new byte[PipeFrame.MaxFrameSize];

        private Queue<PipeFrame> frameQueue = new Queue<PipeFrame>();

        private volatile bool isDisposed;
        private volatile bool isClosed = true;

        public bool IsConnected
        {
            get
            {
                if (isClosed) {
                    return false;
                }

                lock (streamLock) {
                    return (stream != null && stream.IsConnected);
                }
            }
        }

        public event Action FrameReceived;

        public bool Connect(int pipe)
        {
            if (isDisposed) {
                throw new ObjectDisposedException(nameof(stream));
            }

            if (pipe > 9) {
                throw new ArgumentOutOfRangeException(nameof(pipe), "Argument cannot be greater than 9");
            }

            if (pipe < 0) {
                for (int i = 0; i < 10; i++) {
                    if (AttemptConnection(i) || AttemptConnection(i, true)) {
                        BeginReadStream();
                        return true;
                    }
                }
            } else {
                if (AttemptConnection(pipe) || AttemptConnection(pipe, true)) {
                    BeginReadStream();
                    return true;
                }
            }

            return false;
        }

        private bool AttemptConnection(int pipe, bool useSandbox = false)
        {
            if (isDisposed) {
                throw new ObjectDisposedException(nameof(stream));
            }

            string sandbox;
            if (useSandbox) {
                switch (Environment.OSVersion.Platform) {
                    default:
                        return false;
                    case PlatformID.Unix:
                        sandbox = "snap.discord/";
                        break;
                }
            } else {
                sandbox = "";
            }

            string pipeName = GetPipeName(pipe, sandbox);

            try {
                lock (streamLock) {
                    stream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                    stream.Connect(1000);

                    do {
                        Thread.Sleep(10);
                    } while (!stream.IsConnected);
                }

                connectedPipe = pipe;
                isClosed = false;
            } catch {
                Close();
            }

            return !isClosed;
        }

        private void BeginReadStream()
        {
            if (isClosed) {
                return;
            }

            try {
                lock (streamLock) {
                    if (stream == null || !stream.IsConnected) {
                        return;
                    }

                    stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(EndReadStream), stream.IsConnected);
                }
            } catch (ObjectDisposedException) {
                return;
            } catch (InvalidOperationException) {
                // The pipe has been closed
                return;
            } catch {
                // Unknown error
            }
        }

        private void EndReadStream(IAsyncResult callback)
        {
            int bytes = 0;

            try {
                lock (streamLock) {
                    if (stream == null || !stream.IsConnected) {
                        return;
                    }

                    bytes = stream.EndRead(callback);
                }
            } catch (IOException) {
                // The pipe has been closed
                return;
            } catch (NullReferenceException) {
                return;
            } catch (ObjectDisposedException) {
                return;
            } catch {
                // Unknown error
                return;
            }

            if (bytes > 0) {
                using (MemoryStream memory = new MemoryStream(buffer, 0, bytes)) {
                    try {
                        PipeFrame frame = new PipeFrame();
                        if (frame.ReadStream(memory)) {

                            lock (frameQueue) {
                                frameQueue.Enqueue(frame);
                            }

                            FrameReceived?.Invoke();
                        } else {
                            Close();
                        }
                    } catch {
                        Close();
                    }
                }
            }

            if (!isClosed && IsConnected) {
                BeginReadStream();
            }
        }

        public bool ReadFrame(out PipeFrame frame)
        {
            if (isDisposed) {
                throw new ObjectDisposedException(nameof(stream));
            }

            lock (frameQueue) {
                if (frameQueue.Count == 0) {
                    frame = default(PipeFrame);
                    return false;
                }

                frame = frameQueue.Dequeue();
                return true;
            }
        }

        public bool WriteFrame(PipeFrame frame)
        {
            if (isDisposed) {
                throw new ObjectDisposedException(nameof(stream));
            }

            if (isClosed || !IsConnected) {
                return false;
            }

            try {
                frame.WriteStream(stream);
                return true;
            } catch {
                // Nothing to do...
                return false;
            }
        }

        public void Close()
        {
            if (isClosed) {
                return;
            }

            try {
                lock (streamLock) {
                    if (stream != null) {
                        try {
                            stream.Flush();
                            stream.Dispose();
                        } catch {
                            // Nothing to do...
                        }

                        stream = null;
                        isClosed = true;
                    }
                }
            } catch (ObjectDisposedException) {
                // It's already disposed
            } finally {
                isClosed = true;
                connectedPipe = -1;
            }
        }

        public void Dispose()
        {
            if (isDisposed) {
                return;
            }

            isDisposed = true;

            if (!isClosed) {
                Close();
            }

            lock (streamLock) {
                if (stream != null) {
                    stream.Dispose();
                    stream = null;
                }
            }
        }

        private string GetPipeName(int pipe, string sandbox)
        {
            switch (Environment.OSVersion.Platform) {
                default:
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    return sandbox + string.Format(PipeName, pipe);

                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    string temp = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR")
                        ?? Environment.GetEnvironmentVariable("TMPDIR")
                        ?? Environment.GetEnvironmentVariable("TMP")
                        ?? Environment.GetEnvironmentVariable("TEMP")
                        ?? "/tmp";
                    return Path.Combine(temp, sandbox + string.Format(PipeName, pipe));
            }
        }
    }
}