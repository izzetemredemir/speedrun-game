namespace Fusion.Animations
{
	using System;
	using UnityEngine;
	using UnityEngine.Animations;
	using UnityEngine.Playables;

	public abstract class MultiClipState : AnimationState, IAnimationConvertor
	{
		// PUBLIC MEMBERS

		[NonSerialized]
		public float AnimationTime;
		[NonSerialized]
		public float InterpolatedAnimationTime;

		// PROTECTED MEMBERS

		protected ClipNode[]             Nodes => _nodes;
		protected AnimationMixerPlayable Mixer => _mixer;

		// PRIVATE MEMBERS

		[SerializeField]
		private ClipNode[] _nodes;

		private AnimationMixerPlayable _mixer;

		// PUBLIC METHODS

		public bool IsFinished(float normalizedTime = 1.0f)
		{
			if (AnimationTime < normalizedTime)
				return false;

			return IsActive();
		}

		// MultiClipState INTERFACE

		protected abstract int GetClipID();

		protected virtual void OnClipRestarted() {}
		protected virtual void OnClipFinished()  {}

		protected virtual void OnConvert(AnimationConvertor convertor) {}

		// AnimationState INTERFACE

		protected override void CreatePlayable()
		{
			_mixer = AnimationMixerPlayable.Create(Controller.Graph, _nodes.Length);

			for (int i = 0, count = _nodes.Length; i < count; ++i)
			{
				ClipNode node = _nodes[i];

				node.CreatePlayable(Controller.Graph);

				_mixer.ConnectInput(i, node.PlayableClip, 0);
			}

			AddPlayable(_mixer, 0);
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
			int clipID = GetClipID();

			for (int i = 0, count = _nodes.Length; i < count; ++i)
			{
				_mixer.SetInputWeight(i, 0.0f);
			}

			_mixer.SetInputWeight(clipID, 1.0f);

			ClipNode node = _nodes[clipID];

			float oldAnimationTime = AnimationTime;
			float newAnimationTime = oldAnimationTime + Controller.DeltaTime * node.Speed / node.Length;
			bool  clipRestarted    = false;

			if (newAnimationTime >= 1.0f)
			{
				if (node.IsLooping == true)
				{
					newAnimationTime %= 1.0f;
				}
				else
				{
					newAnimationTime = 1.0f;
				}

				if (oldAnimationTime < 1.0f)
				{
					clipRestarted = true;
				}
			}

			AnimationTime = newAnimationTime;

			node.PlayableClip.SetTime(newAnimationTime * node.Length);

			if (clipRestarted == true)
			{
				if (node.IsLooping == true)
				{
					OnClipRestarted();
				}
				else
				{
					OnClipFinished();
				}
			}
		}

		protected override void OnInterpolate()
		{
			int clipID = GetClipID();

			for (int i = 0, count = _nodes.Length; i < count; ++i)
			{
				_mixer.SetInputWeight(i, 0.0f);
			}

			_mixer.SetInputWeight(clipID, 1.0f);

			ClipNode node = _nodes[clipID];
			node.PlayableClip.SetTime(InterpolatedAnimationTime * node.Length);
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
	}
}
