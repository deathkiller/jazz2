using System;
using System.Collections.Generic;
using Duality.Components;
using Duality.Resources;

namespace Duality.Audio
{
    /// <summary>
    /// Provides functionality to play and manage sound in Duality.
    /// </summary>
    public sealed class SoundDevice : IDisposable
	{
		private	bool					disposed		= false;
		private	GameObject				soundListener	= null;
		private	List<ISoundInstance>	sounds			= new List<ISoundInstance>();
		private	int						numPlaying2D	= 0;
		private	int						numPlaying3D	= 0;
		private	bool					mute			= false;

		/// <summary>
		/// [GET / SET] The current listener object. This is automatically set to an available
		/// <see cref="Duality.Components.SoundListener"/>.
		/// </summary>
		public GameObject Listener
		{
			get { return this.soundListener; }
			set { this.soundListener = value; }
		}
		/// <summary>
		/// [GET] The current listeners position.
		/// </summary>
		public Vector3 ListenerPos
		{
			get { return (this.soundListener != null && this.soundListener.Transform != null) ? this.soundListener.Transform.Pos : Vector3.Zero; }
		}
		/// <summary>
		/// [GET] The current listeners velocity.
		/// </summary>
		public Vector3 ListenerVel
		{
			get { return (this.soundListener != null && this.soundListener.Transform != null) ? this.soundListener.Transform.Vel : Vector3.Zero; }
		}
		/// <summary>
		/// [GET] The current listeners rotation / angle in radians.
		/// </summary>
		public float ListenerAngle
		{
			get { return (this.soundListener != null && this.soundListener.Transform != null) ? this.soundListener.Transform.Angle : 0.0f; }
		}
		
		/// <summary>
		/// [GET / SET] Whether all Duality audio is currently muted completely.
		/// </summary>
		public bool Mute
		{
			get { return this.mute; }
			set { this.mute = value; }
		}
		/// <summary>
		/// [GET] Returns a <see cref="Duality.Resources.Sound">Sounds</see> default minimum distance.
		/// </summary>
		public float DefaultMinDist
		{
			get { return 200.0f; }
		}
		/// <summary>
		/// [GET] Returns a <see cref="Duality.Resources.Sound">Sounds</see> default maximum distance.
		/// </summary>
		public float DefaultMaxDist
		{
			get { return 900.0f; }
		}
		/// <summary>
		/// [GET] Returns the maximum number of available OpenAL sound sources.
		/// </summary>
		public int MaxOpenALSources
		{
			get { return DualityApp.AudioBackend.MaxSourceCount; }
		}
		/// <summary>
		/// [GET] Returns the number of currently playing 2d sounds.
		/// </summary>
		public int NumPlaying2D
		{
			get { return this.numPlaying2D; }
		}
		/// <summary>
		/// [GET] Returns the number of currently playing 3d sounds.
		/// </summary>
		public int NumPlaying3D
		{
			get { return this.numPlaying3D; }
		}
		/// <summary>
		/// [GET] Returns the number of currently available OpenAL sound sources.
		/// </summary>
		public int NumAvailable
		{
			get { return DualityApp.AudioBackend.AvailableSources; }
		}
		/// <summary>
		/// [GET] Enumerates all currently playing SoundInstances.
		/// </summary>
		public IEnumerable<ISoundInstance> Playing
		{
			get { return this.sounds; }
		}


		public SoundDevice()
		{
            UpdateWorldSettings();
        }
		~SoundDevice()
		{
			this.Dispose(false);
		}
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		private void Dispose(bool manually)
		{
			if (!this.disposed)
			{
				this.disposed = true;

				// Clear all playing sounds
				foreach (ISoundInstance inst in this.sounds) inst.Dispose();
				this.sounds.Clear();
			}
		}

		///// <summary>
		///// Determines the number of playing instances of a specific <see cref="Duality.Resources.Sound"/>.
		///// </summary>
		///// <param name="snd">The Sound of which to determine the number of playing instances.</param>
		///// <returns>The number of the specified Sounds playing instances.</returns>
		//public int GetNumPlaying(ContentRef<Sound> snd)
		//{
        //    int curNumSoundRes;
        //    if (!snd.IsAvailable || snd.IsRuntimeResource || !this.resPlaying.TryGetValue(snd.Path, out curNumSoundRes))
        //    	return 0;
        //    else
        //    	return curNumSoundRes;
		//}
		/// <summary>
		/// Registers a <see cref="Duality.Resources.Sound">Sounds</see> playing instance.
		/// </summary>
		/// <param name="snd">The Sound that is playing.</param>
		/// <param name="is3D">Whether the instance is 3d or not.</param>
		internal void RegisterPlaying(ContentRef<Sound> snd, bool is3D)
		{
			if (is3D)	this.numPlaying3D++;
			else		this.numPlaying2D++;

			//if (snd.IsAvailable && !snd.IsRuntimeResource)
			//{
			//	if (!this.resPlaying.ContainsKey(snd.Path))
			//		this.resPlaying.Add(snd.Path, 1);
			//	else
			//		this.resPlaying[snd.Path]++;
			//}
		}
		/// <summary>
		/// Unregisters a <see cref="Duality.Resources.Sound">Sounds</see> playing instance.
		/// </summary>
		/// <param name="snd">The Sound that was playing.</param>
		/// <param name="is3D">Whether the instance is 3d or not.</param>
		internal void UnregisterPlaying(ContentRef<Sound> snd, bool is3D)
		{
			if (is3D)	this.numPlaying3D--;
			else		this.numPlaying2D--;

			//if (snd.IsAvailable && !snd.IsRuntimeResource)
			//	this.resPlaying[snd.Path]--;
		}
		
		/// <summary>
		/// Updates the SoundDevice.
		/// </summary>
		internal void Update()
		{
			this.UpdateListener();

			for (int i = this.sounds.Count - 1; i >= 0; i--)
			{
				this.sounds[i].Update();
				if (this.sounds[i].IsDisposed) this.sounds.RemoveAt(i);
			}
			this.sounds.Sort((obj1, obj2) => obj2.Priority - obj1.Priority);
		}
		private void UpdateListener()
		{
			if (this.soundListener != null && (this.soundListener.Disposed || !this.soundListener.Active)) this.soundListener = null;

			// If no listener is defined, search one
			if (this.soundListener == null)
			{
				this.soundListener = Scene.Current.FindGameObject<SoundListener>();
			}

			DualityApp.AudioBackend.UpdateListener(
			    this.ListenerPos * AudioUnit.LengthToPhysical,
			    this.ListenerVel * AudioUnit.VelocityToPhysical,
			    this.ListenerAngle * AudioUnit.AngleToPhysical,
                this.mute);
		}
        private void UpdateWorldSettings()
		{
            const float speedOfSound = 360.0f;
            const float soundDopplerFactor = 1.0f;

            DualityApp.AudioBackend.UpdateWorldSettings(
                speedOfSound,           // Already in meters per second / audio units
                soundDopplerFactor);
		}

        /// <summary>
        /// Plays a sound.
        /// </summary>
        /// <param name="snd">The Sound to play.</param>
        /// <returns>A new SoundInstance representing the currentply playing sound.</returns>
        public SoundInstance PlaySound(ContentRef<Sound> snd)
		{
			SoundInstance inst = new SoundInstance(snd);
			this.sounds.Add(inst);
			return inst;
		}
		/// <summary>
		/// Plays a sound 3d "in space".
		/// </summary>
		/// <param name="snd">The Sound to play.</param>
		/// <param name="pos">The position of the sound in space.</param>
		/// <returns>A new SoundInstance representing the currentply playing sound.</returns>
		public SoundInstance PlaySound3D(ContentRef<Sound> snd, Vector3 pos)
		{
			SoundInstance inst = new SoundInstance(snd, pos);
			this.sounds.Add(inst);
			return inst;
		}
		/// <summary>
		/// Plays a sound 3d "in space".
		/// </summary>
		/// <param name="snd">The Sound to play.</param>
		/// <param name="attachTo">The GameObject to which the sound will be attached.</param>
		/// <returns>A new SoundInstance representing the currentply playing sound.</returns>
		public SoundInstance PlaySound3D(ContentRef<Sound> snd, GameObject attachTo)
		{
			SoundInstance inst = new SoundInstance(snd, attachTo);
			this.sounds.Add(inst);
			return inst;
		}
		/// <summary>
		/// Plays a sound 3d "in space".
		/// </summary>
		/// <param name="snd">The Sound to play.</param>
		/// <param name="attachTo">The GameObject to which the sound will be attached.</param>
		/// <param name="relativePos">The position of the sound relative to the GameObject.</param>
		/// <returns>A new SoundInstance representing the currentply playing sound.</returns>
		public SoundInstance PlaySound3D(ContentRef<Sound> snd, GameObject attachTo, Vector3 relativePos)
		{
			SoundInstance inst = new SoundInstance(snd, attachTo);
			inst.Pos = relativePos;
			this.sounds.Add(inst);
			return inst;
		}

	    public TSoundInstance PlaySound<TSoundInstance>(TSoundInstance snd) where TSoundInstance : ISoundInstance
	    {
	        this.sounds.Add(snd);
	        return snd;
	    }

        /// <summary>
        /// Stops all currently playing sounds.
        /// </summary>
        public void StopAll()
		{
			for (int i = this.sounds.Count - 1; i >= 0; i--)
			{
				this.sounds[i].Stop();
			}
		}
    }
}