using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Fusion.KCC;

namespace TPSBR
{
	using Fusion;
	using Fusion.Animations;

	public sealed unsafe class TurnState : AnimationState, IAnimationConvertor
	{
		// PUBLIC MEMBERS

		public float BlendSpeed => _blendSpeed;
		public float TurnSpeed  => _turnSpeed;

		[NonSerialized][AnimationProperty(nameof(InterpolateAnimationTime))]
		public float AnimationTime;
		[NonSerialized][AnimationProperty(nameof(InterpolateRemainingTime))]
		public float RemainingTime;
		[NonSerialized]
		public float InterpolatedAnimationTime;
		[NonSerialized]
		public float InterpolatedRemainingTime;

		// PRIVATE MEMBERS

		[SerializeField]
		private ClipNode[] _nodes;
		[SerializeField]
		private float      _blendSpeed = 1.0f;
		[SerializeField]
		private float      _turnSpeed = 1.0f;
		[SerializeField]
		private float      _animationPower = 1.0f;
		[SerializeField]
		private float      _maxAnimationSpeed = 1.0f;

		private KCC                    _kcc;
		private Agent                  _agent;
		private Weapons                _weapons;
		private AnimationMixerPlayable _mixer;

		// PUBLIC METHODS

		public void Refresh(float angle)
		{
			if (angle < 0.0f && RemainingTime <= 0.0f)
			{
				RemainingTime = Mathf.Clamp(RemainingTime + angle * _turnSpeed, -_maxAnimationSpeed, 0.0f);
			}
			if (angle > 0.0f && RemainingTime >= 0.0f)
			{
				RemainingTime = Mathf.Clamp(RemainingTime + angle * _turnSpeed, 0.0f, _maxAnimationSpeed);
			}
		}

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

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_kcc     = Controller.GetComponentNoAlloc<KCC>();
			_agent   = Controller.GetComponentNoAlloc<Agent>();
			_weapons = Controller.GetComponentNoAlloc<Weapons>();
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
			int idleClipID = GetClipID();
			int turnClipID;

			float remainingTime = Mathf.Abs(RemainingTime);

			float remainingDeltaTime = Controller.DeltaTime * Mathf.Max(0.5f, remainingTime);
			float animationDeltaTime = Controller.DeltaTime * remainingTime;

			if (RemainingTime <= 0.0f)
			{
				turnClipID = idleClipID + 1;
				RemainingTime = Mathf.Clamp(RemainingTime + remainingDeltaTime * _blendSpeed, -_maxAnimationSpeed, 0.0f);
			}
			else
			{
				turnClipID = idleClipID + 2;
				RemainingTime = Mathf.Clamp(RemainingTime - remainingDeltaTime * _blendSpeed, 0.0f, _maxAnimationSpeed);
			}

			for (int i = 0, count = _nodes.Length; i < count; ++i)
			{
				_mixer.SetInputWeight(i, 0.0f);
			}

			ClipNode idleNode = _nodes[idleClipID];
			ClipNode turnNode = _nodes[turnClipID];

			float animationTime = AnimationTime + animationDeltaTime * turnNode.Speed / turnNode.Length;
			if (animationTime >= 1.0f)
			{
				animationTime %= 1.0f;
			}

			AnimationTime = animationTime;

			_mixer.SetInputWeight(idleClipID, 1.0f - _animationPower);
			_mixer.SetInputWeight(turnClipID, _animationPower);

			idleNode.PlayableClip.SetTime(animationTime * idleNode.Length);
			turnNode.PlayableClip.SetTime(animationTime * turnNode.Length);
		}

		protected override void OnInterpolate()
		{
			int idleClipID = GetClipID();
			int turnClipID = InterpolatedRemainingTime <= 0.0f ? (idleClipID + 1) : (idleClipID + 2);

			for (int i = 0, count = _nodes.Length; i < count; ++i)
			{
				_mixer.SetInputWeight(i, 0.0f);
			}

			_mixer.SetInputWeight(idleClipID, 1.0f - _animationPower);
			_mixer.SetInputWeight(turnClipID, _animationPower);

			ClipNode idleNode = _nodes[idleClipID];
			idleNode.PlayableClip.SetTime(InterpolatedAnimationTime * idleNode.Length);

			ClipNode turnNode = _nodes[turnClipID];
			turnNode.PlayableClip.SetTime(InterpolatedAnimationTime * turnNode.Length);
		}

		protected override void OnDeactivate()
		{
			RemainingTime = 0.0f;
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
		}

		// PRIVATE METHODS

		private int GetClipID()
		{
			int currentWeaponSlot = _weapons.CurrentWeaponSlot;
			if (currentWeaponSlot > 2)
			{
				currentWeaponSlot = 1; // For grenades we use pistol set
			}

			if (currentWeaponSlot < 0)
				return 0;

			return currentWeaponSlot * 3;
		}

		private void InterpolateAnimationTime(InterpolationData interpolationData)
		{
			InterpolatedAnimationTime = AnimationUtility.InterpolateTime(*((float*)interpolationData.From), *((float*)interpolationData.To), 1.0f, interpolationData.Alpha, InterpolatedWeight);
		}

		private void InterpolateRemainingTime(InterpolationData interpolationData)
		{
			InterpolatedRemainingTime = Mathf.LerpUnclamped(*((float*)interpolationData.From), *((float*)interpolationData.To), interpolationData.Alpha);
		}
	}
}
