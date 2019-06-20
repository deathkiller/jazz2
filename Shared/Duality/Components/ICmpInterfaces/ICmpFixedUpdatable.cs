namespace Duality
{
	/// <summary>
	/// Implement this interface in <see cref="Component">Components</see> that require updates with fixed period.
	/// </summary>
	public interface ICmpFixedUpdatable
	{
		/// <summary>
		/// Called once per fixed period in order to update the Component.
		/// </summary>
		void OnFixedUpdate(float timeMult);
	}
}
