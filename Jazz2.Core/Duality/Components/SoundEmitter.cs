using System.Collections.Generic;
using Duality.Audio;
using Duality.Resources;

namespace Duality.Components
{
    /// <summary>
    /// Provides functionality to emit sound.
    /// </summary>
    //[RequiredComponent(typeof(Transform))]
	public sealed class SoundEmitter : Component, ICmpUpdatable, ICmpInitializable//, ICmpEditorUpdatable
	{
		/// <summary>
		/// A single sound source.
		/// </summary>
		public class Source
		{
			private	ContentRef<Sound>	sound			= null;
			private	bool				looped			= true;
			private	bool				paused			= false;
			private	float				volume			= 1.0f;
			private	float				pitch			= 1.0f;
			private	float				lowpass			= 1.0f;
			private	Vector3				offset			= Vector3.Zero;
			private	bool				hasBeenPlayed	= false;
			private	SoundInstance	    instance		= null;

			/// <summary>
			/// [GET] The <see cref="SoundInstance"/> that is currently allocated to emit
			/// this sources sound.
			/// </summary>
			public SoundInstance Instance
			{
				get { return this.instance; }
			}
			/// <summary>
			/// [GET / SET] The <see cref="Duality.Resources.Sound"/> that is to be played by this source.
			/// </summary>
			public ContentRef<Sound> Sound
			{
				get { return this.sound; }
				set { this.sound = value; }
			}
			/// <summary>
			/// [GET / SET] Whether this source is looped.
			/// </summary>
			public bool Looped
			{
				get { return this.looped; }
				set 
				{ 
					if (this.instance != null) this.instance.Looped = value;
					this.looped = value;
				}
			}
			/// <summary>
			/// [GET / SET] Whether this source is paused.
			/// </summary>
			public bool Paused
			{
				get { return this.paused; }
				set 
				{ 
					if (this.instance != null) this.instance.Paused = value;
					this.paused = value;
				}
			}
			/// <summary>
			/// [GET / SET] The volume of this source.
			/// </summary>
			public float Volume
			{
				get { return this.volume; }
				set 
				{ 
					if (this.instance != null) this.instance.Volume = value;
					this.volume = value;
				}
			}
			/// <summary>
			/// [GET / SET] The sources pitch factor.
			/// </summary>
			public float Pitch
			{
				get { return this.pitch; }
				set 
				{ 
					if (this.instance != null) this.instance.Pitch = value;
					this.pitch = value;
				}
			}
			/// <summary>
			/// [GET / SET] The sources lowpass factor.
			/// </summary>
			public float Lowpass
			{
				get { return this.lowpass; }
				set 
				{ 
					if (this.instance != null) this.instance.Lowpass = value;
					this.lowpass = value;
				}
			}
			/// <summary>
			/// [GET / SET] The 3d offset of the emitted sound relative to the GameObject.
			/// </summary>
			public Vector3 Offset
			{
				get { return this.offset; }
				set
				{
					if (this.instance != null) this.instance.Pos = value;
					this.offset = value;
				}
			}

			public Source() {}
			public Source(ContentRef<Sound> snd, bool looped = true) : this(snd, looped, Vector3.Zero) {}
			public Source(ContentRef<Sound> snd, bool looped, Vector3 offset)
			{
				this.sound = snd;
				this.looped = looped;
				this.offset = offset;
			}

			/// <summary>
			/// Updates the sound source.
			/// </summary>
			/// <param name="emitter">The sources parent <see cref="SoundEmitter"/>.</param>
			/// <returns>True, if the source is still active. False, if it requests to be removed.</returns>
			public bool Update(SoundEmitter emitter)
			{
				// Revalidate Sound reference
				this.sound.MakeAvailable();

				// If the SoundInstance has been disposed, set to null
				if (this.instance != null && this.instance.IsDisposed) this.instance = null;

				// If there is a SoundInstance playing, but it's the wrong one, stop it
				if (this.instance != null && this.instance.Sound != this.sound)
				{
					this.instance.Stop();
					this.instance = null;
				}

				if (this.instance == null)
				{
					// If this Source isn't looped and it HAS been played already, remove it
					if (!this.looped && this.hasBeenPlayed) return false;

					// Play the sound
					this.instance = DualityApp.Sound.PlaySound3D(this.sound, emitter.GameObj);
					this.instance.Pos = this.offset;
					this.instance.Looped = this.looped;
					this.instance.Volume = this.volume;
					this.instance.Pitch = this.pitch;
					this.instance.Lowpass = this.lowpass;
					this.instance.Paused = this.paused;
					this.hasBeenPlayed = true;
				}

				return true;
			}
		}

		private List<Source> sources = new List<Source>();

		/// <summary>
		/// [GET / SET] A list of sound sources this SoundEmitter maintains. Is never null.
		/// </summary>
		public List<Source> Sources
		{
			get { return this.sources; }
			set { this.sources = value ?? new List<Source>(); }
		}

		void ICmpUpdatable.OnUpdate()
		{
			for (int i = this.sources.Count - 1; i >= 0; i--)
				if (this.sources[i] != null && !this.sources[i].Update(this)) this.sources.RemoveAt(i);
		}
		/*void ICmpEditorUpdatable.OnUpdate()
		{
			if (DualityApp.ExecContext != DualityApp.ExecutionContext.Game)
			{
				for (int i = this.sources.Count - 1; i >= 0; i--)
					if (this.sources[i].Instance != null) this.sources[i].Instance.Stop();
			}
		}*/
		void ICmpInitializable.OnInit(InitContext context)
		{
		}
		void ICmpInitializable.OnShutdown(ShutdownContext context)
		{
			if (context == ShutdownContext.Deactivate)
			{
				for (int i = this.sources.Count - 1; i >= 0; i--)
					if (this.sources[i].Instance != null) this.sources[i].Instance.Stop();
			}
		}
	}
}
