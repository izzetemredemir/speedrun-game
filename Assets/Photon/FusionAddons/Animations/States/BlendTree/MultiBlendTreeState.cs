namespace Fusion.Animations
{
	using System;
	using UnityEngine;
	using UnityEngine.Animations;
	using UnityEngine.Playables;

	public abstract class MultiBlendTreeState : AnimationState, IAnimationConvertor
	{
		// PUBLIC MEMBERS

		[NonSerialized]
		public float   AnimationTime;
		[NonSerialized]
		public float[] Weights;
		[NonSerialized]
		public float   InterpolatedAnimationTime;
		[NonSerialized]
		public float[] InterpolatedWeights;

		// PROTECTED MEMBERS

		protected BlendTreeSet[]         Sets  => _sets;
		protected AnimationMixerPlayable Mixer => _mixer;

		// PRIVATE MEMBERS

		[SerializeField]
		private BlendTreeSet[] _sets;
		[SerializeField]
		private bool           _isLooping;
		[SerializeField]
		private float          _blendTime;

		private AnimationMixerPlayable _mixer;
		private float[]                _cachedWeights;

		// PUBLIC METHODS

		public bool IsFinished(float normalizedTime = 1.0f)
		{
			if (AnimationTime < normalizedTime)
				return false;

			return IsActive();
		}

		// MultiBlendTreeState INTERFACE

		public abstract Vector2 GetBlendPosition(bool interpolated);
		public abstract float   GetSpeedMultiplier();

		protected abstract int GetSetID();

		protected virtual void OnConvert(AnimationConvertor convertor) {}

		// AnimationState INTERFACE

		protected override void CreatePlayable()
		{
			_mixer = AnimationMixerPlayable.Create(Controller.Graph, _sets.Length);

			for (int i = 0; i < _sets.Length; ++i)
			{
				BlendTreeSet set = _sets[i];
				set.CreatePlayable(Controller);

				_mixer.ConnectInput(i, set.Mixer, 0);
			}

			AddPlayable(_mixer, 0);
		}

		protected override void OnInitialize()
		{
			Weights             = new float[_sets.Length];
			InterpolatedWeights = new float[_sets.Length];
			_cachedWeights      = new float[_sets.Length];
		}

		protected override void OnSpawned()
		{
			for (int i = 0; i < _sets.Length; ++i)
			{
				Weights[i]             = 0.0f;
				InterpolatedWeights[i] = 0.0f;
				_cachedWeights[i]      = 0.0f;

				BlendTreeSet set = _sets[i];
				set.ResetSpeed();
			}
		}

		protected override void OnDespawned()
		{
			if (_mixer.IsValid() == true)
			{
				_mixer.Destroy();
			}

			for (int i = 0; i < _sets.Length; ++i)
			{
				BlendTreeSet set = _sets[i];
				set.DestroyPlayable();
			}
		}

		protected override void OnFixedUpdate()
		{
			int     setID         = GetSetID();
			Vector2 blendPosition = GetBlendPosition(false);

			float targetLength = 0.0f;
			float targetWeight = 0.0f;
			float totalWeight  = 0.0f;

			for (int i = 0; i < _sets.Length; ++i)
			{
				float weight = Weights[i];

				if (setID == i)
				{
					weight = _blendTime > 0.0f ? Mathf.Min(weight + Controller.DeltaTime / _blendTime, 1.0f) : 1.0f;
				}
				else
				{
					weight = _blendTime > 0.0f ? Mathf.Max(0.0f, weight - Controller.DeltaTime / _blendTime) : 0.0f;
				}

				if (weight > 0.0f)
				{
					float clipLength = _sets[i].SetPosition(blendPosition);
					if (clipLength > 0.0f)
					{
						targetLength += clipLength * weight;
						targetWeight += weight;
					}

					totalWeight += weight;
				}

				Weights[i] = weight;
			}

			if (targetWeight > 0.0f && targetLength > 0.0f)
			{
				targetLength /= targetWeight;

				float speedMultiplier     = GetSpeedMultiplier();
				float normalizedDeltaTime = Controller.DeltaTime * speedMultiplier / targetLength;

				AnimationTime += normalizedDeltaTime;
				if (AnimationTime > 1.0f)
				{
					if (_isLooping == true)
					{
						AnimationTime %= 1.0f;
					}
					else
					{
						AnimationTime = 1.0f;
					}
				}
			}

			float weightMultiplier = totalWeight > 0.0f ? 1.0f / totalWeight : 0.0f;

			for (int i = 0; i < _sets.Length; ++i)
			{
				float weight = Weights[i] * weightMultiplier;
				if (weight > 0.0f)
				{
					BlendTreeSet set = _sets[i];
					set.SetTime(AnimationTime);
				}

				if (weight != _cachedWeights[i])
				{
					_cachedWeights[i] = weight;
					_mixer.SetInputWeight(i, weight);
				}
			}
		}

		protected override void OnInterpolate()
		{
			Vector2 blendPosition = GetBlendPosition(true);

			float totalWeight = 0.0f;

			for (int i = 0; i < _sets.Length; ++i)
			{
				totalWeight += InterpolatedWeights[i];
			}

			float weightMultiplier = totalWeight > 0.0f ? 1.0f / totalWeight : 0.0f;

			for (int i = 0; i < _sets.Length; ++i)
			{
				float weight = InterpolatedWeights[i] * weightMultiplier;
				if (weight > 0.0f)
				{
					BlendTreeSet set = _sets[i];
					set.SetPosition(blendPosition);
					set.SetTime(InterpolatedAnimationTime);
				}

				if (weight != _cachedWeights[i])
				{
					_cachedWeights[i] = weight;
					_mixer.SetInputWeight(i, weight);
				}
			}
		}

		protected override void OnSetDefaults()
		{
			AnimationTime = 0.0f;
		}

		// IAnimationConvertor INTERFACE

		void IAnimationConvertor.Convert(AnimationConvertor convertor)
		{
			for (int i = 0; i < _sets.Length; ++i)
			{
				_sets[i].Convert(convertor);
			}

			OnConvert(convertor);
		}
	}
}
