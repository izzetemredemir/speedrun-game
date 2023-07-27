using System.Collections;
using UnityEngine;

namespace TPSBR
{
	public static class AudioSourceExtensions
	{
		// CONSTANTS 

		private const float MIN_DURATION = 0.05f;

		// PUBLIC METHODS

		public static Coroutine FadeIn(this AudioSource source, MonoBehaviour behavior, float duration = 1f, float delay = 0f, float volume = 1f)
		{
			if (duration < MIN_DURATION && delay <= 0f)
			{
				source.Play();
				source.volume = volume;
				return null;
			}

			source.volume = 0f;
			return behavior.StartCoroutine(Fade_Coroutine(source, volume, duration, delay));
		}

		public static Coroutine FadeOut(this AudioSource source, MonoBehaviour behavior, float duration = 1f, float delay = 0f)
		{
			if ((duration < MIN_DURATION && delay <= 0f) || source.isPlaying == false)
			{
				source.Stop();
				source.volume = 0f;
				return null;
			}

			return behavior.StartCoroutine(Fade_Coroutine(source, 0f, duration, delay));
		}

		public static Coroutine CrossFade(this AudioSource source, MonoBehaviour behavior, AudioClip clip, float fadeOut = 1f, float fadeIn = 1f, float fadeOutDelay = 0f, float fadeInDelay = 0f, float volume = 1f)
		{
			if (fadeOut < MIN_DURATION || source.isPlaying == false)
			{
				source.Stop();
				source.clip = clip;
				source.volume = 0f;
				return FadeIn(source, behavior, fadeIn, fadeOutDelay + fadeOut + fadeInDelay, volume);
			}

			return behavior.StartCoroutine(CrossFade_Coroutine(source, clip, fadeOut, fadeIn, fadeOutDelay, fadeInDelay, volume));
		}

		// PRIVATE METHOD

		private static IEnumerator Fade_Coroutine(AudioSource source, float targetVolume, float duration, float delay)
		{
			if (delay > 0f)
			{
				yield return new WaitForSeconds(delay);
			}

			float startVolume = source.volume;
			float time = 0f;

			if (source.isPlaying == false)
			{
				source.Play();
			}

			while (time < duration)
			{
				source.volume = Mathf.Lerp(startVolume, targetVolume, time / duration);
				time += Time.deltaTime;

				yield return null;
			}

			source.volume = targetVolume;

			if (targetVolume == 0f)
			{
				source.Stop();
			}
		}

		private static IEnumerator CrossFade_Coroutine(AudioSource source, AudioClip clip, float fadeOut, float fadeIn, float fadeOutDelay, float fadeInDelay, float volume)
		{
			yield return Fade_Coroutine(source, 0f, fadeOut, fadeOutDelay);

			source.clip = clip;

			yield return Fade_Coroutine(source, volume, fadeIn, fadeInDelay);
		}
	}
}
