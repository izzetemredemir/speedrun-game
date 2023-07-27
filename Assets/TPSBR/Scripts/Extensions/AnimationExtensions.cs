using UnityEngine;

namespace TPSBR
{
	public static class AnimationExtensions
	{
		public static void PlayForward(this Animation animation, bool reset = false)
		{
			animation.PlayForward(animation.clip, reset);
		}

		public static void PlayForward(this Animation animation, AnimationClip clip, bool reset = false)
		{
			animation.Play(clip.name, 1f, reset);
		}

		public static void PlayForward(this Animation animation, string clipName, bool reset = false)
		{
			animation.Play(clipName, 1f, reset);
		}

		public static void PlayBackward(this Animation animation, bool reset = false)
		{
			animation.PlayBackward(animation.clip, reset);
		}

		public static void PlayBackward(this Animation animation, AnimationClip clip, bool reset = false)
		{
			animation.Play(clip.name, -1f, reset);
		}

		public static void PlayBackward(this Animation animation, string clipName, bool reset = false)
		{
			animation.Play(clipName, -1f, reset);
		}

		public static void Play(this Animation animation, string clipName, float speed, bool reset = false)
		{
			var state = animation[clipName];

			bool isPlaying = state.enabled == true && state.weight > 0f;

			if (isPlaying == false || reset == true)
			{
				state.time = speed >= 0f ? 0f : state.length;
			}

			state.speed = speed;

			if (speed != 0f)
			{
				state.enabled = true;
				state.weight = 1f;
			}
		}

		public static void SampleStart(this Animation animation)
		{
			animation.SampleStart(animation.clip.name);
		}

		public static void SampleEnd(this Animation animation)
		{
			animation.SampleEnd(animation.clip.name);
		}

		public static void SampleStart(this Animation animation, string clipName)
		{
			animation.Sample(clipName, 0f);
		}

		public static void SampleEnd(this Animation animation, string clipName)
		{
			animation.Sample(clipName, 1f);
		}

		public static void Sample(this Animation animation, string clipName, float normalizedTime)
		{
			animation.Stop();

			var state = animation[clipName];

			state.normalizedTime = normalizedTime;

			state.weight = 1f;
			state.enabled = true;

			animation.Sample();

			state.enabled = false;
		}
	}
}
