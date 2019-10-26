using System;
using Duality.Backend;

namespace Duality.Audio
{
    [Flags]
    public enum SoundInstanceFlags
    {
        None = 0,
        /// <summary>Whether the sound is played in a loop.</summary>
        Looped = 1 << 0,

        GameplaySpecific = 1 << 1,
    }

    public interface ISoundInstance : IDisposable, IAudioStreamProvider
    {
        SoundInstanceFlags Flags { get; }
        bool IsDisposed { get; }
        bool Paused { get; set; }
        int Priority { get; }

        void Stop();

        void Update();

        void FadeTo(float target, float timeSeconds);

        void BeginFadeIn(float timeSeconds);

        void FadeIn(float timeSeconds);

        void FadeOut(float timeSeconds);

        void StopFade();
    }
}