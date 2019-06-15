namespace Duality.Async
{
	public struct WaitForRawFrames : IAwaitInstruction
	{
		private float finishFrame;

		bool IAwaitInstruction.IsCompleted() => finishFrame <= AsyncManager.CurrentRawFrameCount;
		
		/// <summary>
		/// Waits for the specified number of frames to pass before continuing
		/// </summary>
		public WaitForRawFrames(int count)
		{
			finishFrame = AsyncManager.CurrentRawFrameCount + count;
		}
		
		public Continuation<WaitForRawFrames> GetAwaiter() => new Continuation<WaitForRawFrames>(this);
	}
}