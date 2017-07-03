using System;
using Duality.Backend;

namespace Duality.Audio
{
    public interface ISoundInstance : IDisposable, IAudioStreamProvider
    {
        bool IsDisposed { get; }
        int Priority { get; }

        void Stop();

        void Update();
    }
}