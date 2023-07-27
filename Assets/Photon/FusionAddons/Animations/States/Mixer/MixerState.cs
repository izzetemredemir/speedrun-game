namespace Fusion.Animations
{
	using System.Collections.Generic;
	using UnityEngine.Animations;
	using UnityEngine.Playables;

	public class MixerState : AnimationState, IAnimationStateOwner
	{
		// PRIVATE MEMBERS

		private AnimationMixerPlayable _mixer;

		// AnimationState INTERFACE

		protected override void CreatePlayable()
		{
			_mixer = AnimationMixerPlayable.Create(Controller.Graph, States.Count);
			AddPlayable(_mixer, 0);
		}

		protected override void OnDespawned()
		{
			if (_mixer.IsValid() == true)
			{
				_mixer.Destroy();
			}
		}

		// IAnimationStateOwner INTERFACE

		AnimationMixerPlayable IAnimationStateOwner.Mixer => _mixer;

		void IAnimationStateOwner.Activate(AnimationState source, float duration)
		{
			IList<AnimationState> states = States;
			for (int i = 0, count = states.Count; i < count; ++i)
			{
				AnimationState state = states[i];
				if (state.Port != source.Port)
				{
					state.Deactivate(duration, true);
				}
			}

			if ((FadingSpeed == 0.0f && Weight > 0.0f) || FadingSpeed > 0.0f)
				return;

			if (duration <= 0.0f)
			{
				Weight      = 1.0f;
				FadingSpeed = 0.0f;
			}
			else
			{
				FadingSpeed = 1.0f / duration;
			}

			AnimationController.Log($"{nameof(MixerState)}.{nameof(Activate)} ({name}), Duration: {duration.ToString("F3")}", gameObject);

			OnActivate();

			Owner.Activate(this, duration);
		}

		void IAnimationStateOwner.Deactivate(AnimationState source, float duration)
		{
			if ((FadingSpeed == 0.0f && Weight <= 0.0f) || FadingSpeed < 0.0f)
				return;

			if (duration <= 0.0f)
			{
				Weight      = 0.0f;
				FadingSpeed = 0.0f;
			}
			else
			{
				FadingSpeed = 1.0f / -duration;
			}

			AnimationController.Log($"{nameof(MixerState)}.{nameof(Deactivate)} ({name}), Duration: {duration.ToString("F3")}", gameObject);

			OnDeactivate();

			Owner.Deactivate(this, duration);
		}
	}
}
