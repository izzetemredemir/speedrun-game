namespace Fusion.Animations
{
	public unsafe abstract class AnimationNetworkProperty
	{
		// PUBLIC MEMBERS

		public readonly int WordCount;

		// CONSTRUCTORS

		public AnimationNetworkProperty(int wordCount)
		{
			WordCount = wordCount;

			//UnityEngine.Debug.LogWarning($"Added network property ({wordCount})");
		}

		// AnimationNetworkProperty INTERFACE

		public abstract void Read(int* ptr);
		public abstract void Write(int* ptr);
		public abstract void Interpolate(InterpolationData interpolationData);
	}
}
