namespace Fusion.Animations
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Animations;
	using UnityEngine.Playables;

	public abstract unsafe partial class AnimationLayer : MonoBehaviour, IAnimationStateOwner
	{
		// PUBLIC MEMBERS

		public AnimationController    Controller => _controller;
		public IList<AnimationState>  States     => _states;
		public AnimationMixerPlayable Mixer      => _mixer;
		public int                    Port       => _port;

		[NonSerialized]
		public float Weight;
		[NonSerialized]
		public float FadingSpeed;
		[NonSerialized]
		public float InterpolatedWeight;

		// PRIVATE MEMBERS

		[SerializeField]
		private AvatarMask             _avatarMask;
		[SerializeField]
		private bool                   _isAdditive;
		[SerializeField]
		public float                   _weight;

		private AnimationController    _controller;
		private AnimationState[]       _states;
		private AnimationMixerPlayable _mixer;
		private string                 _type;
		private int                    _port;
		private float                  _cachedWeight;

		// PUBLIC METHODS

		public bool IsActive()
		{
			return (FadingSpeed == 0.0f && Weight > 0.0f) || FadingSpeed > 0.0f;
		}

		public bool IsPlaying()
		{
			return FadingSpeed > 0.0f || Weight > 0.0f;
		}

		public bool IsFadingIn()
		{
			return FadingSpeed > 0.0f;
		}

		public bool IsFadingOut()
		{
			return FadingSpeed < 0.0f;
		}

		public void Activate(float duration)
		{
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

			AnimationController.Log($"{nameof(AnimationLayer)}.{nameof(Activate)} ({name}), Duration: {duration.ToString("F3")}", gameObject);

			OnActivate();
		}

		public void Deactivate(float duration)
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

			AnimationController.Log($"{nameof(AnimationLayer)}.{nameof(Deactivate)} ({name}), Duration: {duration.ToString("F3")}", gameObject);

			OnDeactivate();
		}

		public bool HasActiveState()
		{
			AnimationState[] states = _states;
			for (int i = 0, count = states.Length; i < count; ++i)
			{
				AnimationState state = states[i];
				if (state.IsActive(true) == true)
					return true;
			}

			return false;
		}

		public AnimationState GetActiveState()
		{
			AnimationState[] states = _states;
			for (int i = 0, count = states.Length; i < count; ++i)
			{
				AnimationState state = states[i];
				if (state.IsActive(true) == true)
					return state;
			}

			return null;
		}

		public void DeactivateAllStates(float duration)
		{
			AnimationState[] states = _states;
			for (int i = 0, count = states.Length; i < count; ++i)
			{
				AnimationState state = states[i];
				state.Deactivate(duration, true);
			}
		}

		public void Initialize(AnimationController controller)
		{
			_controller = controller;
			_type       = GetType().Name;

			InitializeStates();

			OnInitialize();
		}

		public void Deinitialize()
		{
			OnDeinitialize();

			DeinitializeStates();

			_controller = default;
		}

		public void Spawned()
		{
			_mixer        = AnimationMixerPlayable.Create(_controller.Graph);
			_port         = _controller.Mixer.AddInput(_mixer, 0, _weight);
			_cachedWeight = _weight;

			_controller.Mixer.SetLayerAdditive((uint)_port, _isAdditive);

			if (_avatarMask != null)
			{
				_controller.Mixer.SetLayerMaskFromAvatarMask((uint)_port, _avatarMask);
			}

			Weight             = _weight;
			FadingSpeed        = 0.0f;
			InterpolatedWeight = _weight;

			AnimationState[] states = _states;
			for (int i = 0, count = states.Length; i < count; ++i)
			{
				AnimationState state = states[i];
				state.Spawned();
			}

			OnSpawned();
		}

		public void Despawned()
		{
			OnDespawned();

			AnimationState[] states = _states;
			for (int i = 0, count = states != null ? states.Length : 0; i < count; ++i)
			{
				AnimationState state = states[i];
				if (state != null)
				{
					state.Despawned();
				}
			}

			if (_mixer.IsValid() == true)
			{
				_mixer.Destroy();
			}
		}

		public void ManualFixedUpdate()
		{
			if (FadingSpeed <= 0.0f && Weight <= 0.0f)
				return;

#if ANIMATION_PROFILING
			UnityEngine.Profiling.Profiler.BeginSample(_type);
#endif
			AnimationState[] states = _states;
			for (int i = 0, count = states.Length; i < count; ++i)
			{
				AnimationState state = states[i];
				state.ManualFixedUpdate();
			}

			if (FadingSpeed != 0.0f)
			{
				Weight += FadingSpeed * _controller.DeltaTime;

				if (Weight <= 0.0f)
				{
					Weight      = 0.0f;
					FadingSpeed = 0.0f;
				}
				else if (Weight >= 1.0f)
				{
					Weight      = 1.0f;
					FadingSpeed = 0.0f;
				}
			}

			OnFixedUpdate();

#if ANIMATION_PROFILING
			UnityEngine.Profiling.Profiler.EndSample();
#endif
		}

		public void Interpolate()
		{
			if (InterpolatedWeight <= 0.0f)
				return;

#if ANIMATION_PROFILING
			UnityEngine.Profiling.Profiler.BeginSample(_type);
#endif
			AnimationState[] states = _states;
			for (int i = 0, count = states.Length; i < count; ++i)
			{
				AnimationState state = states[i];
				state.Interpolate();
			}

			OnInterpolate();
#if ANIMATION_PROFILING
			UnityEngine.Profiling.Profiler.EndSample();
#endif
		}

		public float GetPlayableWeight()
		{
			return _controller.Mixer.GetInputWeight(_port);
		}

		public void SetPlayableWeights(bool interpolated)
		{
			float layerWeight = interpolated == true ? InterpolatedWeight : Weight;
			if (layerWeight <= 0.0f)
			{
				SetPlayableWeight(0.0f);
				return;
			}

			AnimationState[] states     = _states;
			int              stateCount = states.Length;

			if (stateCount == 0)
			{
				SetPlayableWeight(layerWeight);
				return;
			}

			if (stateCount == 1)
			{
				AnimationState state = states[0];
				float playableWeight = state.CalculatePlayableWeights(interpolated, out float maxChildWeight);
				state.SetPlayableWeight(playableWeight > 0.0f ? 1.0f : 0.0f);
				SetPlayableWeight(maxChildWeight * layerWeight);
				return;
			}

			float maxWeight      = 0.0f;
			float childrenWeight = 0.0f;

			for (int i = 0; i < stateCount; ++i)
			{
				childrenWeight += states[i].CalculatePlayableWeights(interpolated, out float maxChildWeight);

				if (maxChildWeight > maxWeight)
				{
					maxWeight = maxChildWeight;
				}
			}

			if (childrenWeight == 1.0f || childrenWeight == 0.0f)
			{
				for (int i = 0; i < stateCount; ++i)
				{
					states[i].ApplyPlayableWeight();
				}
			}
			else
			{
				float weightMultiplier = 1.0f / childrenWeight;

				for (int i = 0; i < stateCount; ++i)
				{
					states[i].ApplyPlayableWeight(weightMultiplier);
				}

				if (childrenWeight > 1.0f)
				{
					childrenWeight = 1.0f;
				}
			}

			if (childrenWeight > maxWeight)
			{
				maxWeight = childrenWeight;
			}

			SetPlayableWeight(maxWeight * layerWeight);
		}

		private void SetPlayableWeight(float weight)
		{
			if (weight == _cachedWeight)
				return;

			_cachedWeight = weight;
			_controller.Mixer.SetInputWeight(_port, weight);
		}

		public T FindState<T>() where T : class
		{
			return FindState<T>(out T state) == true ? state : default;
		}

		public bool FindState<T>(out T state) where T : class
		{
			AnimationState[] states = _states;
			for (int i = 0, count = states.Length; i < count; ++i)
			{
				AnimationState layerState = states[i];
				if (layerState is T layerStateAsT)
				{
					state = layerStateAsT;
					return true;
				}

				if (layerState.FindState<T>(out T innerState) == true)
				{
					state = innerState;
					return true;
				}
			}

			state = default;
			return false;
		}

		// AnimationLayer INTERFACE

		protected virtual void OnInitialize()   {}
		protected virtual void OnDeinitialize() {}
		protected virtual void OnSpawned()      {}
		protected virtual void OnDespawned()    {}
		protected virtual void OnFixedUpdate()  {}
		protected virtual void OnInterpolate()  {}
		protected virtual void OnActivate()     {}
		protected virtual void OnDeactivate()   {}

		// IAnimationStateOwner INTERFACE

		AnimationMixerPlayable IAnimationStateOwner.Mixer => _mixer;

		bool IAnimationStateOwner.IsActive(bool self)
		{
			return (FadingSpeed == 0.0f && Weight > 0.0f) || FadingSpeed > 0.0f;
		}

		bool IAnimationStateOwner.IsPlaying(bool self)
		{
			return FadingSpeed > 0.0f || Weight > 0.0f;
		}

		bool IAnimationStateOwner.IsFadingIn(bool self)
		{
			return FadingSpeed > 0.0f;
		}

		bool IAnimationStateOwner.IsFadingOut(bool self)
		{
			return FadingSpeed < 0.0f;
		}

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
		}

		void IAnimationStateOwner.Deactivate(AnimationState source, float duration)
		{
		}

		// PRIVATE METHODS

		private void InitializeStates()
		{
			if (_states != null)
				return;

			List<AnimationState> activeStates = new List<AnimationState>(8);

			Transform root = transform;
			for (int i = 0, count = root.childCount; i < count; ++i)
			{
				Transform child = root.GetChild(i);

				AnimationState state = child.GetComponentNoAlloc<AnimationState>();
				if (state != null && state.enabled == true && state.gameObject.activeSelf == true)
				{
					activeStates.Add(state);
				}
			}

			_states = activeStates.ToArray();

			AnimationState[] states = _states;
			for (int i = 0, count = states != null ? states.Length : 0; i < count; ++i)
			{
				AnimationState state = states[i];
				state.Initialize(_controller, this);
			}
		}

		private void DeinitializeStates()
		{
			AnimationState[] states = _states;
			for (int i = 0, count = states != null ? states.Length : 0; i < count; ++i)
			{
				AnimationState state = states[i];
				if (state != null)
				{
					state.Deinitialize();
				}
			}

			_states = null;
		}
	}
}
