namespace Fusion.Animations
{
	using System;
	using UnityEngine;
	using UnityEngine.Animations;
	using UnityEngine.Playables;

	public abstract class BlendTreeState : AnimationState, IAnimationConvertor
	{
		// PUBLIC MEMBERS

		[NonSerialized]
		public float AnimationTime;
		[NonSerialized]
		public float InterpolatedAnimationTime;

		// PROTECTED MEMBERS

		protected BlendTreeNode[]        Nodes => _nodes;
		protected AnimationMixerPlayable Mixer => _mixer;

		// PRIVATE MEMBERS

		[SerializeField]
		private float           _speed;
		[SerializeField]
		private BlendTreeNode[] _nodes;
		[SerializeField]
		private bool            _isLooping;

		private AnimationMixerPlayable _mixer;
		private AnimationBlendTree     _blendTree;
		private bool                   _isCacheValid;
		private float                  _cachedTargetLength;
		private Vector2                _cachedPosition;

		// PUBLIC METHODS

		public bool IsFinished(float normalizedTime = 1.0f)
		{
			if (AnimationTime < normalizedTime)
				return false;

			return IsActive();
		}

		public void SetSpeed(float speed)
		{
			_blendTree.SetScale(speed);
			_isCacheValid = false;
		}

		// BlendTreeState INTERFACE

		protected abstract Vector2 GetBlendPosition(bool interpolated);
		protected abstract float   GetSpeedMultiplier();

		protected virtual void OnConvert(AnimationConvertor convertor) {}

		// AnimationState INTERFACE

		protected override void CreatePlayable()
		{
			int nodeCount = _nodes.Length;

			_mixer = AnimationMixerPlayable.Create(Controller.Graph, _nodes.Length);

			Vector2[] blendTreePositions = new Vector2[nodeCount];

			for (int i = 0; i < nodeCount; ++i)
			{
				BlendTreeNode node = _nodes[i];

				node.CreatePlayable(Controller.Graph);
				blendTreePositions[i] = node.Position;

				_mixer.ConnectInput(i, node.PlayableClip, 0);
			}

			_blendTree = new AnimationBlendTree(blendTreePositions);

			AddPlayable(_mixer, 0);
		}

		protected override void OnSpawned()
		{
			SetSpeed(_speed);
		}

		protected override void OnDespawned()
		{
			if (_mixer.IsValid() == true)
			{
				_mixer.Destroy();
			}

			for (int i = 0, count = _nodes.Length; i < count; ++i)
			{
				_nodes[i].DestroyPlayable();
			}
		}

		protected override void OnFixedUpdate()
		{
			Vector2 blendPosition = GetBlendPosition(false);
			AnimationTime = SetPosition(blendPosition, AnimationTime, Controller.DeltaTime * GetSpeedMultiplier());
		}

		protected override void OnInterpolate()
		{
			Vector2 blendPosition = GetBlendPosition(true);
			SetPosition(blendPosition, InterpolatedAnimationTime, 0.0f);
		}

		protected override void OnSetDefaults()
		{
			AnimationTime = 0.0f;
		}

		// IAnimationConvertor INTERFACE

		void IAnimationConvertor.Convert(AnimationConvertor convertor)
		{
			for (int i = 0; i < _nodes.Length; ++i)
			{
				_nodes[i].Convert(convertor);
			}

			OnConvert(convertor);
		}

		// PRIVATE METHODS

		private float SetPosition(Vector2 position, float animationTime, float deltaTime)
		{
			float targetLength = 0.0f;

			if (_isCacheValid == true && AlmostEquals(position, _cachedPosition, 0.01f) == true)
			{
				targetLength = _cachedTargetLength;
			}
			else
			{
				_blendTree.CalculateWeights(position);

				float[] weights = _blendTree.Weights;

				for (int i = 0, count = _nodes.Length; i < count; ++i)
				{
					float weight = weights[i];
					if (weight > 0.0f)
					{
						targetLength += _nodes[i].Length / _nodes[i].Speed * weight;
					}

					_mixer.SetInputWeight(i, weight);
				}

				_isCacheValid       = true;
				_cachedPosition     = position;
				_cachedTargetLength = targetLength;
			}

			if (targetLength >= 0.001f)
			{
				deltaTime /= targetLength;
			}

			animationTime += deltaTime;
			if (animationTime > 1.0f)
			{
				if (_isLooping == true)
				{
					animationTime %= 1.0f;
				}
				else
				{
					animationTime = 1.0f;
				}
			}

			for (int i = 0, count = _nodes.Length; i < count; ++i)
			{
				if (_blendTree.Weights[i] > 0.0f)
				{
					BlendTreeNode node = _nodes[i];
					node.PlayableClip.SetTime(animationTime * node.Length);
				}
			}

			return animationTime;
		}

		// PRIVATE METHODS

		private static bool AlmostEquals(Vector2 vectorA, Vector2 vectorB, float tolerance = 0.01f)
		{
			Vector2 difference = vectorA - vectorB;
			return difference.x < tolerance && difference.x > -tolerance && difference.y < tolerance && difference.y > -tolerance;
		}
	}
}
