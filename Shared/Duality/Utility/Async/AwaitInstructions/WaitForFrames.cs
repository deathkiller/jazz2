namespace Duality.Async
{
	public struct WaitForFrames : IAwaitInstruction
	{
		private float finishFrame;

		bool IAwaitInstruction.IsCompleted() => finishFrame <= AsyncManager.CurrentFrameCount;

		/// <summary>
		/// Waits for the specified number of scaled frames to pass before continuing
		/// </summary>
		public WaitForFrames(int count)
		{
			finishFrame = AsyncManager.CurrentFrameCount + count;
		}

		public Continuation<WaitForFrames> GetAwaiter() => new Continuation<WaitForFrames>(this);
	}
}