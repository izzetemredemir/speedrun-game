namespace TPSBR
{
	public static class AudioEffectExtensions
	{
		public static bool PlaySound(this AudioEffect[] effects, AudioSetup setup, EForceBehaviour force = EForceBehaviour.None)
		{
			if (effects == null)
				return false;

			AudioEffect bestPlayingEffect = null;
			float bestTime = 0.5f;

			for (int i = 0; i < effects.Length; i++)
			{
				var audioEffect = effects[i];

				if (audioEffect.IsPlaying == false)
				{
					audioEffect.Play(setup);
					return true;
				}

				bool chooseAudioEffect = false;

				switch (force)
				{
					case EForceBehaviour.ForceDifferentSetup:
						chooseAudioEffect = audioEffect.AudioSource.time > bestTime && audioEffect.CurrentSetup != setup;
						break;
					case EForceBehaviour.ForceSameSetup:
						chooseAudioEffect = audioEffect.AudioSource.time > bestTime && audioEffect.CurrentSetup == setup;
						break;
					case EForceBehaviour.ForceAny:
						chooseAudioEffect = audioEffect.AudioSource.time > bestTime;
						break;
				}

				if (chooseAudioEffect == true)
				{
					bestPlayingEffect = audioEffect;
					bestTime = audioEffect.AudioSource.time;
				}
			}

			if (force == EForceBehaviour.None)
				return false; // No free audio effect

			if (bestPlayingEffect != null)
			{
				bestPlayingEffect.Play(setup, force);
				return true;
			}

			return false;
		}
	}
}
