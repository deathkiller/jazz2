namespace Duality.Components
{
	/// <summary>
	/// Makes this <see cref="GameObject"/> the 3d sound listener.
	/// </summary>
	//[RequiredComponent(typeof(Transform))]
	public sealed class SoundListener : Component, ICmpInitializable
	{
		public void MakeCurrent()
		{
			if (!this.Active) {
				return;
			}

			DualityApp.Sound.Listener = this.GameObj;
		}

		void ICmpInitializable.OnInit(InitContext context)
		{
			if (context == InitContext.Activate) {
				this.MakeCurrent();
			}
		}
		void ICmpInitializable.OnShutdown(ShutdownContext context)
		{
		}
	}
}
