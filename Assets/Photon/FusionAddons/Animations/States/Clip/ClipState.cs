namespace Fusion.Animations
{
	using System;
	using UnityEngine;
	using UnityEngine.Playables;

	public class ClipState : AnimationState, IAnimationConvertor
	{
		// PUBLIC MEMBERS

		[NonSerialized]
		public float AnimationTime;
		[NonSerialized]
		public float InterpolatedAnimationTime;

		// PROTECTED MEMBERS

		protected ClipNode Node => _node;

		// PRIVATE MEMBERS

		[SerializeField]
		private ClipNode _node;

		// PUBLIC METHODS

		public bool IsFinished(float normalizedTime = 1.0f)
		{
			if (AnimationTime < normalizedTime)
				return false;

			return IsActive();
		}

		// ClipState INTERFACE

		protected virtual void OnClipRestarted() {}
		protected virtual void OnClipFinished()  {}

		protected virtual void OnConvert(AnimationConvertor convertor) {}

		// AnimationState INTERFACE

		protected override void CreatePlayable()
		{
			_node.CreatePlayable(Controller.Graph);
			AddPlayable(_node.PlayableClip, 0);
		}

		protected override void OnDespawned()
		{
			_node.DestroyPlayable();
		}

		protected override void OnFixedUpdate()
		{
			float oldAnimationTime = AnimationTime;
			float newAnimationTime = oldAnimationTime + Controller.DeltaTime * _node.Speed / _node.Length;
			bool  clipRestarted    = false;

			if (newAnimationTime >= 1.0f)
			{
				if (_node.IsLooping == true)
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

			_node.PlayableClip.SetTime(newAnimationTime * _node.Length);

			if (clipRestarted == true)
			{
				if (_node.IsLooping == true)
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
			_node.PlayableClip.SetTime(InterpolatedAnimationTime * _node.Length);
		}

		protected override void OnSetDefaults()
		{
			AnimationTime = 0.0f;
		}

		// IAnimationConvertor INTERFACE

		void IAnimationConvertor.Convert(AnimationConvertor convertor)
		{
			_node.Convert(convertor);

			OnConvert(convertor);
		}
	}
}
