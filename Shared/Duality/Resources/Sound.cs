using System.Collections.Generic;
using System.Linq;
using Duality.Audio;

namespace Duality.Resources
{
	/// <summary>
	/// A Sound is parameterized <see cref="Duality.Resources.AudioData"/>. Note that a Sound
	/// Resource does not contain any kind of audio data by itsself.
	/// </summary>
	/// <example>
	/// While there may be one AudioData Resource sounding like an Explosion effect, there could 
	/// be many Sounds referring to it, like a very loud or very quiet Explosion or one that is 
	/// noticable on higher distance than others.
	/// </example>
	/// <seealso cref="Duality.Resources.AudioData"/>
	public class Sound : Resource
	{
		private int maxInstances = 5;
		private float minDistFactor = 1.0f;
		private float maxDistFactor = 1.0f;
		private float volFactor = 1.0f;
		private float pitchFactor = 1.0f;
		private float lowpassFactor = 1.0f;
		private float fadeOutAt = 0.0f;
		private float fadeOutTime = 0.0f;
		private SoundType type = SoundType.World;
		private List<ContentRef<AudioData>> audioData = null;

		/// <summary>
		/// [GET / SET] A collection of <see cref="Duality.Resources.AudioData"/>, which are used by this Sound.
		/// </summary>
		public List<ContentRef<AudioData>> Data
		{
			get { return this.audioData; }
			set
			{
				this.audioData = value;
				this.PreloadData();
			}
		}
		/// <summary>
		/// [GET / SET] The main source <see cref="Duality.Resources.AudioData"/> that is used by this Sound.
		/// </summary>
		public ContentRef<AudioData> MainData
		{
			get { return this.audioData != null && this.audioData.Count > 0 ? this.audioData[0] : null; }
			set
			{
				if (this.audioData == null)
					this.audioData = new List<ContentRef<AudioData>>();

				if (this.audioData.Count == 0)
					this.audioData.Add(value);
				else
					this.audioData[0] = value;
			}
		}
		/// <summary>
		/// [GET / SET] The category to which this Sound belongs.
		/// </summary>
		public SoundType Type
		{
			get { return this.type; }
			set { this.type = value; }
		}
		/// <summary>
		/// [GET / SET] Maximum number of <see cref="Duality.Audio.SoundInstance">SoundInstances</see> of this Sound that can
		/// play simultaneously. If exceeded, any new instances of it are discarded.
		/// </summary>
		public int MaxInstances
		{
			get { return this.maxInstances; }
			set { this.maxInstances = value; }
		}
		/// <summary>
		/// [GET / SET] A volume factor that is applied when playing this sound.
		/// </summary>
		public float VolumeFactor
		{
			get { return this.volFactor; }
			set { this.volFactor = value; }
		}
		/// <summary>
		/// [GET / SET] A pitch factor that is applied when playing this sound.
		/// </summary>
		public float PitchFactor
		{
			get { return this.pitchFactor; }
			set { this.pitchFactor = value; }
		}
		/// <summary>
		/// [GET / SET] A lowpass factor that is applied when playing this sound.
		/// </summary>
		public float LowpassFactor
		{
			get { return this.lowpassFactor; }
			set { this.lowpassFactor = value; }
		}
		/// <summary>
		/// [GET / SET] Play time in seconds at which <see cref="Duality.Audio.SoundInstance">SoundInstances</see> of this Sound
		/// automatically fade out.
		/// </summary>
		public float FadeOutAt
		{
			get { return this.fadeOutAt; }
			set { this.fadeOutAt = value; }
		}
		/// <summary>
		/// [GET / SET] If <see cref="FadeOutAt"/> has been triggered, this is the fade out time in seconds that is used.
		/// </summary>
		public float FadeOutTime
		{
			get { return this.fadeOutTime; }
			set { this.fadeOutTime = value; }
		}
		/// <summary>
		/// [GET / SET] The distance at which the sound is played at full volume. Getting nearer to the source won't increase the volume anymore.
		/// Note that the value is a factor to <see cref="SoundDevice.DefaultMinDist"/>.
		/// </summary>
		/// <seealso cref="MinDist"/>
		public float MinDistFactor
		{
			get { return this.minDistFactor; }
			set { this.minDistFactor = value; }
		}
		/// <summary>
		/// [GET / SET] The distance at which the sound is played at zero volume.
		/// Note that the value is a factor to <see cref="SoundDevice.DefaultMaxDist"/>.
		/// </summary>
		/// <seealso cref="MaxDist"/>
		public float MaxDistFactor
		{
			get { return this.maxDistFactor; }
			set { this.maxDistFactor = value; }
		}
		/// <summary>
		/// [GET / SET] The distance at which the sound is played at full volume. Getting nearer to the source won't increase the volume anymore.
		/// </summary>
		/// <seealso cref="MinDistFactor"/>
		public float MinDist
		{
			get { return DualityApp.Sound.DefaultMinDist * this.minDistFactor; }
			set { this.minDistFactor = value / DualityApp.Sound.DefaultMinDist; }
		}
		/// <summary>
		/// [GET / SET] The distance at which the sound is played at zero volume.
		/// </summary>
		/// <seealso cref="MaxDistFactor"/>
		public float MaxDist
		{
			get { return DualityApp.Sound.DefaultMaxDist * this.maxDistFactor; }
			set { this.maxDistFactor = value / DualityApp.Sound.DefaultMaxDist; }
		}

		/// <summary>
		/// Creates a new Sound referring to an existing <see cref="Duality.Resources.AudioData"/>.
		/// </summary>
		/// <param name="baseData"></param>
		public Sound(ContentRef<AudioData> baseData)
		{
			this.audioData = new List<ContentRef<AudioData>> { baseData };
			this.PreloadData();
		}
		/// <summary>
		/// Creates a new Sound referring to an existing set of <see cref="Duality.Resources.AudioData"/>.
		/// </summary>
		/// <param name="baseData"></param>
		public Sound(IEnumerable<ContentRef<AudioData>> baseData)
		{
			this.audioData = baseData.ToList();
			this.PreloadData();
		}

		/// <summary>
		/// Upon playing the Sound, this method is called once to determine which of the referenced
		/// <see cref="Duality.Resources.AudioData"/> objects is to be played.
		/// </summary>
		/// <returns></returns>
		public ContentRef<AudioData> FetchData()
		{
			return MathF.Rnd.OneOf(this.audioData);
		}

		private void PreloadData()
		{
			if (this.audioData == null) return;
			for (int i = 0; i < this.audioData.Count; i++) {
				this.audioData[i].MakeAvailable();
			}
		}
	}
}
