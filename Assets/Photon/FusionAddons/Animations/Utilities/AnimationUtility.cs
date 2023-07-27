namespace Fusion.Animations
{
	using UnityEngine;

	public static partial class AnimationUtility
	{
		// PUBLIC METHODS

		public static float InterpolateWeight(float from, float to, float alpha)
		{
			float distance = to - from;

			if (distance == 1.0f || distance == -1.0f)
				return alpha < 0.5f ? from : to;

			return from + distance * alpha;
		}

		public static float InterpolateTime(float from, float to, float length, float alpha)
		{
			float time;

			if (to >= from)
			{
				time = Mathf.Lerp(from, to, alpha);
			}
			else
			{
				time = Mathf.Lerp(from, to + length, alpha);
				if (time > length)
				{
					time -= length;
				}
			}

			return time;
		}

		public static float InterpolateTime(float from, float to, float length, float alpha, float weight)
		{
			if (weight <= 0.0f)
				return alpha < 0.5f ? from : to;

			float time;

			if (to >= from)
			{
				time = Mathf.Lerp(from, to, alpha);
			}
			else
			{
				time = Mathf.Lerp(from, to + length, alpha);
				if (time > length)
				{
					time -= length;
				}
			}

			return time;
		}
	}
}
