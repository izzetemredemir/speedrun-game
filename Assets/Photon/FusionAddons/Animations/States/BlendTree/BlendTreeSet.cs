namespace Fusion.Animations
{
	using System;
	using UnityEngine;
	using UnityEngine.Animations;
	using UnityEngine.Playables;

	[Serializable]
	public sealed class BlendTreeSet
	{
		// PUBLIC MEMBERS

		public BlendTreeNode[]        Nodes     => _nodes;
		public AnimationMixerPlayable Mixer     => _mixer;
		public AnimationBlendTree     BlendTree => _blendTree;

		// PRIVATE MEMBERS

		[SerializeField]
		private float           _speed;
		[SerializeField]
		private BlendTreeNode[] _nodes;

		private AnimationMixerPlayable _mixer;
		private AnimationBlendTree     _blendTree;

		private bool    _isCacheValid;
		private float   _cachedTargetLength;
		private Vector2 _cachedPosition;

		// PUBLIC METHODS

		public void CreatePlayable(AnimationController controller)
		{
			int nodeCount = _nodes.Length;

			_mixer = AnimationMixerPlayable.Create(controller.Graph, _nodes.Length);

			Vector2[] blendTreePositions = new Vector2[nodeCount];

			for (int i = 0; i < nodeCount; ++i)
			{
				BlendTreeNode node = _nodes[i];

				node.CreatePlayable(controller.Graph);
				blendTreePositions[i] = node.Position;

				_mixer.ConnectInput(i, node.PlayableClip, 0);
			}

			_blendTree = new AnimationBlendTree(blendTreePositions);
		}

		public void DestroyPlayable()
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

		public void SetSpeed(float speed)
		{
			_blendTree.SetScale(speed);

			_isCacheValid = false;
		}

		public void ResetSpeed()
		{
			SetSpeed(_speed);
		}

		public float SetPosition(Vector2 position)
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

			return targetLength;
		}

		public void SetTime(float animationTime)
		{
			for (int i = 0, count = _nodes.Length; i < count; ++i)
			{
				if (_blendTree.Weights[i] > 0.0f)
				{
					BlendTreeNode node = _nodes[i];
					node.PlayableClip.SetTime(animationTime * node.Length);
				}
			}
		}

		public void Convert(AnimationConvertor convertor)
		{
			for (int i = 0; i < _nodes.Length; ++i)
			{
				_nodes[i].Convert(convertor);
			}
		}

		// PRIVATE METHODS

		private static bool AlmostEquals(Vector2 vectorA, Vector2 vectorB, float tolerance = 0.01f)
		{
			Vector2 difference = vectorA - vectorB;
			return difference.x < tolerance && difference.x > -tolerance && difference.y < tolerance && difference.y > -tolerance;
		}
	}
}
