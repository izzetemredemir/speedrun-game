namespace Fusion.Animations
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Animations;
	using UnityEngine.Playables;

	public interface IAnimationStateOwner
	{
		AnimationMixerPlayable Mixer { get; }

		bool IsActive(bool self);
		bool IsPlaying(bool self);
		bool IsFadingIn(bool self);
		bool IsFadingOut(bool self);
		void Activate(AnimationState source, float duration);
		void Deactivate(AnimationState source, float duration);
	}

	public abstract unsafe partial class AnimationState : MonoBehaviour
	{
		// PUBLIC MEMBERS

		public AnimationController   Controller => _controller;
		public IList<AnimationState> States     => _states;
		public int                   Port       => _port;

		[NonSerialized]
		public float Weight;
		[NonSerialized]
		public float FadingSpeed;
		[NonSerialized]
		public float InterpolatedWeight;

		// PROTECTED MEMBERS

		protected IAnimationStateOwner Owner => _owner;

		// PRIVATE MEMBERS

		private AnimationController  _controller;
		private AnimationState[]     _states;
		private IAnimationStateOwner _owner;
		private string               _type;
		private int                  _port;
		private float                _cachedWeight;
		private float                _playableWeight;

		// PUBLIC METHODS

		public bool IsActive(bool self = false)
		{
			return ((FadingSpeed == 0.0f && Weight > 0.0f) || FadingSpeed > 0.0f) && (self == true || _owner.IsActive(false) == true);
		}

		public bool IsPlaying(bool self = false)
		{
			return (FadingSpeed > 0.0f || Weight > 0.0f) && (self == true || _owner.IsPlaying(false) == true);
		}

		public bool IsFadingIn(bool self = false)
		{
			return FadingSpeed > 0.0f && (self == true || (_owner.IsPlaying(false) == true && _owner.IsFadingOut(false) == false));
		}

		public bool IsFadingOut(bool self = false)
		{
			return FadingSpeed < 0.0f && (self == true || (_owner.IsPlaying(false) == true && _owner.IsFadingIn(false) == false));
		}

		public void Activate(float duration, bool self = false)
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

			AnimationController.Log($"{nameof(AnimationState)}.{nameof(Activate)} ({name}), Duration: {duration.ToString("F3")}, Self: {self}", gameObject);

			OnActivate();

			if (self == false)
			{
				_owner.Activate(this, duration);
			}
		}

		public void Deactivate(float duration, bool self = false)
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

			AnimationController.Log($"{nameof(AnimationState)}.{nameof(Deactivate)} ({name}), Duration: {duration.ToString("F3")}, Self: {self}", gameObject);

			OnDeactivate();

			if (self == false)
			{
				_owner.Deactivate(this, duration);
			}
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

		public void SetDefaults()
		{
			Weight      = 0.0f;
			FadingSpeed = 0.0f;

			for (int i = 0, count = _states.Length; i < count; ++i)
			{
				_states[i].SetDefaults();
			}

			OnSetDefaults();
		}

		public void Initialize(AnimationController controller, IAnimationStateOwner owner)
		{
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));

			_controller = controller;
			_owner      = owner;
			_type       = GetType().Name;

			InitializeStates();

			OnInitialize();
		}

		public void Deinitialize()
		{
			OnDeinitialize();

			DeinitializeStates();

			_owner      = default;
			_controller = default;
		}

		public void Spawned()
		{
			Weight             = 0.0f;
			FadingSpeed        = 0.0f;
			InterpolatedWeight = 0.0f;

			_port           = -1;
			_cachedWeight   = 0.0f;
			_playableWeight = 0.0f;

			CreatePlayable();

			if (_port < 0)
				throw new NotSupportedException();

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
			for (int i = 0, count = states.Length; i < count; ++i)
			{
				AnimationState state = states[i];
				if (state != null)
				{
					state.Despawned();
				}
			}
		}

		public void ManualFixedUpdate()
		{
			if (FadingSpeed <= 0.0f && Weight <= 0.0f)
			{
				SetDefaults();
				return;
			}

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
			return _owner.Mixer.GetInputWeight(_port);
		}

		public float CalculatePlayableWeights(bool interpolated, out float maxWeight)
		{
			float stateWeight = interpolated == true ? InterpolatedWeight : Weight;
			if (stateWeight <= 0.0f)
			{
				_playableWeight = 0.0f;
				maxWeight = 0.0f;
				return 0.0f;
			}

			AnimationState[] states     = _states;
			int              stateCount = states.Length;

			if (stateCount == 0)
			{
				_playableWeight = stateWeight;
				maxWeight = stateWeight;
				return stateWeight;
			}

			if (stateCount == 1)
			{
				AnimationState state = states[0];
				_playableWeight = state.CalculatePlayableWeights(interpolated, out float maxChildWeight);
				state.SetPlayableWeight(_playableWeight > 0.0f ? 1.0f : 0.0f);
				maxWeight = maxChildWeight;
				return stateWeight;
			}

			maxWeight = 0.0f;

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

			_playableWeight = childrenWeight;

			return stateWeight;
		}

		public void ApplyPlayableWeight()
		{
			float weight = _playableWeight;
			if (weight == _cachedWeight)
				return;

			_cachedWeight = weight;
			_owner.Mixer.SetInputWeight(_port, weight);
		}

		public void ApplyPlayableWeight(float multiplier)
		{
			float weight = _playableWeight * multiplier;
			if (weight == _cachedWeight)
				return;

			_cachedWeight = weight;
			_owner.Mixer.SetInputWeight(_port, weight);
		}

		public void SetPlayableWeight(float weight)
		{
			if (weight == _cachedWeight)
				return;

			_cachedWeight = weight;
			_owner.Mixer.SetInputWeight(_port, weight);
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
				AnimationState stateState = states[i];
				if (stateState is T stateStateAsT)
				{
					state = stateStateAsT;
					return true;
				}

				if (stateState.FindState<T>(out T innerState) == true)
				{
					state = innerState;
					return true;
				}
			}

			state = default;
			return false;
		}

		// AnimationState INTERFACE

		protected abstract void CreatePlayable();

		protected virtual void OnInitialize()   {}
		protected virtual void OnDeinitialize() {}
		protected virtual void OnSpawned()      {}
		protected virtual void OnDespawned()    {}
		protected virtual void OnFixedUpdate()  {}
		protected virtual void OnInterpolate()  {}
		protected virtual void OnActivate()     {}
		protected virtual void OnDeactivate()   {}
		protected virtual void OnSetDefaults()  {}

		// PROTECTED METHODS

		protected void AddPlayable<T>(T playable, int sourceOutputIndex) where T : struct, IPlayable
		{
			if (_port >= 0)
				throw new NotSupportedException();

			_port = _owner.Mixer.AddInput(playable, sourceOutputIndex, 0.0f);
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
				state.Initialize(_controller, this as IAnimationStateOwner);
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
