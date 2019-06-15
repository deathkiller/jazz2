namespace Duality.Async
{
	public enum FrameScheduler { BeforeUpdate, AfterUpdate }

	/// <summary>
	/// Allows awaitable instructions to be implemented without the use of abstract classes and heap allocations.
	/// For maximum versatility, any struct which implements this should have a <c>public Continuation{T} GetAwaiter()</c>
	/// method exposed. See <see cref="Duality.Async.WaitForRawFrames"/> for a concise example.
	/// </summary>
	public interface IAwaitInstruction
	{
		bool IsCompleted();
	}
}