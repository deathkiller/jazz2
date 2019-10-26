using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using Duality;
using Duality.Audio;
using Duality.Backend;
using Duality.IO;

namespace Jazz2
{
    public class OpenMptStream : Disposable, ISoundInstance
    {
#if PLATFORM_ANDROID
        private const int SampleRate = 44100;
#else
        private const int SampleRate = 48000;
#endif
        private const int BufferSize = 4096 * 2; // 4k buffer per channel, low latency is not needed here...

        public static Version LibraryVersion
        {
            get
            {
                try {
                    int raw = openmpt_get_library_version();
                    return new Version((raw >> 24) & 0xff, (raw >> 16) & 0xff, (raw >> 8) & 0xff, raw & 0xff);
                } catch {
                    return null;
                }
            }
        }

        private Stream stream;
        private openmpt_stream_callbacks stream_callbacks;
        private IntPtr openmpt_module;

        private ushort[] audioBuffer;
        private INativeAudioSource native;

        private bool notYetAssigned = true;

        private float curFade = 1.0f;
        private float fadeTarget = 1.0f;
        private float fadeTimeSec = 1.0f;
        private float fadeWaitEnd = 0.0f;
        private float lowpass = 1.0f;

        public int Priority => int.MaxValue;

        /// <summary>
        /// [GET / SET] The sounds local lowpass value. Lower values cut off more frequencies.
        /// </summary>
        public float Lowpass
        {
            get { return this.lowpass; }
            set { this.lowpass = value; }
        }

        public SoundInstanceFlags Flags => SoundInstanceFlags.None;

        public bool Paused { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public OpenMptStream(string path, bool looping)
        {
            if (!FileOp.Exists(path)) {
                Log.Write(LogType.Warning, "Music file \"" + path + "\" not found!");
                return;
            }

            stream = FileOp.Open(path, FileAccessMode.Read);

            stream_callbacks.read = stream_read_func;
            stream_callbacks.seek = stream_seek_func;
            stream_callbacks.tell = stream_tell_func;

            try {
                // Load module file
                openmpt_module = openmpt_module_create(stream_callbacks, IntPtr.Zero, null, IntPtr.Zero, IntPtr.Zero);

                // Turn on infinite repeat if required
                openmpt_module_set_repeat_count(openmpt_module, looping ? -1 : 0);
            } catch (Exception ex) {
                Log.Write(LogType.Error, "libopenmpt failed to load: " + ex);
                return;
            }

            audioBuffer = new ushort[BufferSize];
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) {
                if (this.native != null) {
                    this.native.Dispose();
                    this.native = null;
                }

                if (openmpt_module != IntPtr.Zero) {
                    openmpt_module_destroy(openmpt_module);
                    openmpt_module = IntPtr.Zero;
                }

                if (stream != null) {
                    stream.Dispose();
                    stream = null;
                }
            }
        }

        public void Stop()
        {
            if (this.native != null) {
                this.native.Stop();
            }
        }

        /// <summary>
        /// Fades the sound to a specific target value.
        /// </summary>
        /// <param name="target">The target value to fade to.</param>
        /// <param name="timeSeconds">The time in seconds the fading will take.</param>
        public void FadeTo(float target, float timeSeconds)
        {
            this.fadeTarget = target;
            this.fadeTimeSec = timeSeconds;
        }
        /// <summary>
        /// Resets the sounds current fade value to zero and starts to fade it in.
        /// </summary>
        /// <param name="timeSeconds">The time in seconds the fading will take.</param>
        public void BeginFadeIn(float timeSeconds)
        {
            this.curFade = 0.0f;
            this.FadeTo(1.0f, timeSeconds);
        }
        /// <summary>
        /// Fades the sound in from its current fade value. Note that SoundInstances are
        /// initialized with a fade value of 1.0f because they aren't faded in generally. 
        /// To achieve a regular "fade in" effect, you should use <see cref="BeginFadeIn(float)"/>.
        /// </summary>
        /// <param name="timeSeconds">The time in seconds the fading will take.</param>
        public void FadeIn(float timeSeconds)
        {
            this.FadeTo(1.0f, timeSeconds);
        }
        /// <summary>
        /// Fades out the sound.
        /// </summary>
        /// <param name="timeSeconds">The time in seconds the fading will take.</param>
        public void FadeOut(float timeSeconds)
        {
            this.FadeTo(0.0f, timeSeconds);
        }
        /// <summary>
        /// Halts the current fading, keepinf the current fade value as fade target.
        /// </summary>
        public void StopFade()
        {
            this.fadeTarget = this.curFade;
        }

        public void Update()
        {
            AudioSourceState nativeState = AudioSourceState.Default;
            nativeState.Volume = SettingsCache.MusicVolume * curFade;
            nativeState.Lowpass = this.lowpass;

            bool fadeOut = this.fadeTarget <= 0.0f;
            if (this.fadeTarget != this.curFade) {
                float fadeTemp = Time.TimeMult * Time.SecondsPerFrame / Math.Max(0.05f, this.fadeTimeSec);

                if (this.fadeTarget > this.curFade) {
                    this.curFade += fadeTemp;
                } else {
                    this.curFade -= fadeTemp;
                }

                if (Math.Abs(this.curFade - this.fadeTarget) < fadeTemp * 2.0f) {
                    this.curFade = this.fadeTarget;
                }
            }

            if (this.notYetAssigned) {
                this.notYetAssigned = false;

                // Grab a native audio source
                if (openmpt_module == IntPtr.Zero) {
                    this.Dispose();
                    return;
                }

                native = DualityApp.AudioBackend.CreateSource();
                native.Play(this);
            }

            // If the source is stopped / finished, dispose and return
            if (this.native == null || this.native.IsFinished) {
                this.Dispose();
                return;
            }

            if (fadeOut && nativeState.Volume <= 0.0f) {
                this.fadeWaitEnd += Time.TimeMult * Time.MillisecondsPerFrame;
                // After fading out entirely, wait 50 ms before actually stopping the source to prevent unpleasant audio tick / glitch noises
                if (this.fadeWaitEnd > 50.0f) {
                    this.Dispose();
                    return;
                }
            } else {
                this.fadeWaitEnd = 0.0f;
            }

            native.ApplyState(ref nativeState);
        }

        #region Internal
        void IAudioStreamProvider.OpenStream()
        {
            // Nothing to do...

            // ToDo: Call openmpt_module_create() here
        }

        bool IAudioStreamProvider.ReadStream(INativeAudioBuffer targetBuffer)
        {
            if (openmpt_module == IntPtr.Zero) {
                return false;
            }

            int count = openmpt_module_read_interleaved_stereo(
                openmpt_module,
                SampleRate,
                BufferSize >> 1, // Buffer size per channel
                audioBuffer
            );
            if (count == 0) {
                return false;
            }

            targetBuffer.LoadData(
                SampleRate,
                audioBuffer,
                count << 1, // Number of samples (left + right)
                AudioDataLayout.LeftRight,
                AudioDataElementType.Short
            );
            return true;
        }

        void IAudioStreamProvider.CloseStream()
        {
            // Nothing to do...

            // ToDo: Call openmpt_module_destroy() here
        }

        private int stream_read_func(IntPtr zero, IntPtr dst, int bytes)
        {
            byte[] buffer = new byte[bytes];
            int read = stream.Read(buffer, 0, bytes);
            Marshal.Copy(buffer, 0, dst, read);
            return read;
        }

        private int stream_seek_func(IntPtr zero, long offset, int origin)
        {
            stream.Seek(offset, (SeekOrigin)origin);
            return 0;
        }

        private long stream_tell_func(IntPtr zero)
        {
            return stream.Position;
        }
        #endregion

        #region Native Methods
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int openmpt_stream_read_func(IntPtr stream, IntPtr dst, int bytes);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int openmpt_stream_seek_func(IntPtr stream, long offset, int whence);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate long openmpt_stream_tell_func(IntPtr stream);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void openmpt_log_func(IntPtr message, IntPtr user);

        [StructLayout(LayoutKind.Sequential)]
        private struct openmpt_stream_callbacks
        {
            public openmpt_stream_read_func read;
            public openmpt_stream_seek_func seek;
            public openmpt_stream_tell_func tell;
        }

        [DllImport("libopenmpt", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private static extern IntPtr openmpt_module_create(openmpt_stream_callbacks stream_callbacks, IntPtr stream, openmpt_log_func logfunc, IntPtr user, /*openmpt_module_initial_ctl**/IntPtr ctls);

        //[DllImport("libopenmpt", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        //private static extern IntPtr openmpt_module_create_from_memory(IntPtr filedata, int filesize, openmpt_log_func logfunc, IntPtr user, /*openmpt_module_initial_ctl**/IntPtr ctls);

        [DllImport("libopenmpt", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private static extern void openmpt_module_destroy(IntPtr mod);

        //[DllImport("libopenmpt", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        //private static extern int openmpt_module_read_stereo(IntPtr mod, int samplerate, int count, ushort[] left, ushort[] right);

        [DllImport("libopenmpt", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private static extern int openmpt_module_read_interleaved_stereo(IntPtr mod, int samplerate, int count, ushort[] interleaved_stereo);

        [DllImport("libopenmpt", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private static extern int openmpt_module_set_repeat_count(IntPtr mod, int repeat_count);
        [DllImport("libopenmpt", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private static extern int openmpt_get_library_version();
        #endregion
    }
}