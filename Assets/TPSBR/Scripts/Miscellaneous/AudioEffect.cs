using System.Collections;
using UnityEngine;

namespace TPSBR
{
	[System.Serializable]
	public class AudioSetup
	{
		public AudioClip[]  Clips;
		public float        Volume = 1f;
		public float        MaxPitchChange;
		public float        Delay;
		public bool         Loop;
		public float        FadeIn;
		public float        FadeOut;

		[Space]
		public bool         Repeat;
		public int          RepeatPlayCount;
		public float        RepeatDelay;

		public void CopyFrom(AudioSetup other)
		{
			Clips           = other.Clips;
			Volume          = other.Volume;
			MaxPitchChange  = other.MaxPitchChange;
			Delay           = other.Delay;
			Loop            = other.Loop;
			FadeIn          = other.FadeIn;
			FadeOut         = other.FadeOut;

			Repeat          = other.Repeat;
			RepeatPlayCount = other.RepeatPlayCount;
			RepeatDelay     = other.RepeatDelay;
		}
	}

	public enum EForceBehaviour
	{
		None,
		ForceAny,
		ForceDifferentSetup,
		ForceSameSetup,
	}

	[RequireComponent(typeof(AudioSource))]
	public sealed class AudioEffect : MonoBehaviour
	{
		// PUBLIC MEMBERS

		public AudioSetup  DefaultSetup        => _defaultSetup;
		public AudioSetup  CurrentSetup        => _currentSetup;
		public AudioSource AudioSource         => _audioSource;
		public bool        IsPlaying           => _audioSource.isPlaying == true || _delayedPlayRoutine != null;

		public int         LastPlayedClipIndex { get; set; }
		public float       BasePitch           { get; set; }

		// PRIVATE MEMBERS

		[SerializeField]
		private AudioSetup  _defaultSetup;

		private AudioSource _audioSource;
		private bool        _playOnAwake;

		private int         _playCount;
		private Coroutine   _delayedPlayRoutine;

		private AudioSetup  _currentSetup;

		// PUBLIC METHODS

		public void Play(EForceBehaviour force = EForceBehaviour.None)
		{
			Play(_defaultSetup, force);
		}

		public void Play(AudioSetup setup, EForceBehaviour force = EForceBehaviour.None)
		{
			if (IsPlaying == true)
			{
				if (force == EForceBehaviour.None)
					return;

				if (force == EForceBehaviour.ForceDifferentSetup && setup == _currentSetup)
					return;

				if (force == EForceBehaviour.ForceSameSetup && setup != _currentSetup)
					return;
			}

			if (setup.Clips == null || setup.Clips.Length == 0)
				return;

			StartPlay(setup);
		}

		public void Stop(bool forceImmediateStop = false)
		{
			StopDelayedPlay();

			if (forceImmediateStop == false && _currentSetup != null && _currentSetup.FadeOut > 0f)
			{
				_audioSource.FadeOut(this, _currentSetup.FadeOut);
			}
			else
			{
				_audioSource.Stop();
			}
		}

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			_audioSource = GetComponent<AudioSource>();

			BasePitch = _audioSource.pitch;

			_playOnAwake = _audioSource.playOnAwake;
			_audioSource.playOnAwake = false;
			_audioSource.Stop();

			_defaultSetup.Loop |= _audioSource.loop;

			if (_defaultSetup.Clips.Length == 0 && _audioSource.clip != null)
			{
				_defaultSetup.Clips = new AudioClip[] { _audioSource.clip };
			}
		}

		private void OnEnable()
		{
			_audioSource.enabled = true;

			if (_playOnAwake == true)
			{
				Play();
			}
		}

		private void OnDisable()
		{
			StopDelayedPlay();
			_audioSource.enabled = false;
		}

		// PRIVATE METHODS

		private void StartPlay(AudioSetup setup)
		{
			AudioSetup previousSetup = _currentSetup;
			_currentSetup = setup;

			LastPlayedClipIndex = NextClipIndex(setup);

			if (LastPlayedClipIndex < 0)
				return;

			if (_currentSetup.Clips[LastPlayedClipIndex] == null)
				return; // Do not start playing if there will be nothing to play

			StopDelayedPlay();
			_playCount = 0;

			bool waitForFadeOut = IsPlaying == true && previousSetup != null && previousSetup.FadeOut > 0.01f;

			if (_currentSetup.Delay < 0.01f && waitForFadeOut == false)
			{
				PlayClip(LastPlayedClipIndex);
			}
			else
			{
				float delay = _currentSetup.Delay;

				if (waitForFadeOut == true)
				{
					delay += previousSetup.FadeOut;
					_audioSource.FadeOut(this, previousSetup.FadeOut);
				}

				_delayedPlayRoutine = StartCoroutine(PlayDelayed_Coroutine(delay, LastPlayedClipIndex));
			}
		}

		private void PlayClip(int clipIndex)
		{
			_audioSource.Stop();
			StopAllCoroutines(); // Stop audiosource fadings

			LastPlayedClipIndex = clipIndex;

			_audioSource.clip = _currentSetup.Clips[clipIndex];
			_audioSource.volume = _currentSetup.Volume;
			_audioSource.loop = _currentSetup.Loop;
			_audioSource.pitch = BasePitch + Random.Range(-_currentSetup.MaxPitchChange, _currentSetup.MaxPitchChange);

			if (_currentSetup.FadeIn > 0f)
			{
				_audioSource.FadeIn(this, _currentSetup.FadeIn, volume: _currentSetup.Volume);
			}
			else
			{
				_audioSource.Play();
			}

			_playCount++;

			if (_currentSetup.Repeat == true && _playCount < _currentSetup.RepeatPlayCount)
			{
				_delayedPlayRoutine = StartCoroutine(PlayDelayed_Coroutine(_audioSource.clip.length + _currentSetup.RepeatDelay, clipIndex));
			}
		}

		private IEnumerator PlayDelayed_Coroutine(float delay, int clipIndex)
		{
			if (delay > 0.01f)
			{
				yield return new WaitForSeconds(delay);
			}

			_delayedPlayRoutine = null;

			PlayClip(clipIndex);
		}

		private void StopDelayedPlay()
		{
			if (_delayedPlayRoutine != null)
			{
				StopCoroutine(_delayedPlayRoutine);
				_delayedPlayRoutine = null;
			}
		}

		private int NextClipIndex(AudioSetup setup)
		{
			if (setup.Clips.Length == 0)
			{
				Debug.LogWarningFormat("Cannot play sound on {0} - missing audio clip", gameObject.name);
				return -1;
			}

			int clipIndex = Random.Range(0, setup.Clips.Length);

			if (clipIndex == LastPlayedClipIndex)
			{
				clipIndex = (clipIndex + 1) % setup.Clips.Length;
			}

			return clipIndex;
		}
	}
}
