namespace Duality.Resources
{
    /// <summary>
    /// Describes how a <see cref="RenderTarget"/> in a rendering setup will be rescaled to fit window- or screen resolution settings.
    /// </summary>
    public struct RenderSetupTargetResize
	{
		/// <summary>
		/// The <see cref="RenderTarget"/> resource to be resized.
		/// </summary>
		public ContentRef<RenderTarget> Target;
		/// <summary>
		/// The <see cref="TargetResize"/> mode that will be applied to the <see cref="RenderTarget"/>.
		/// 
		/// Usually, this should be set to <see cref="TargetResize.None"/>, <see cref="TargetResize.Stretch"/>
		/// or <see cref="TargetResize.Fit"/>.
		/// </summary>
		public TargetResize ResizeMode;
		/// <summary>
		/// An additional scale factor that will be applied to the target size after <see cref="ResizeMode"/>
		/// was taken into account.
		/// </summary>
		public Vector2 Scale;
	}
}
