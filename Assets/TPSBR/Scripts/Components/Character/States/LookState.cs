using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Fusion.KCC;

namespace TPSBR
{
	using Fusion.Animations;

	public sealed class LookState : AnimationState, IAnimationConvertor
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private LookSet[] _sets;

		private KCC                    _kcc;
		private Weapons                _weapons;
		private Jetpack                _jetpack;
		private AnimationMixerPlayable _mixer;
		private int                    _inputs;

		// PUBLIC METHODS

		public bool ShouldActivate()
		{
			return GetSetID() >= 0;
		}

		// AnimationLayer INTERFACE

		protected override void CreatePlayable()
		{
			_mixer  = AnimationMixerPlayable.Create(Controller.Graph, 0);
			_inputs = 0;

			for (int j = 0, setCount = _sets.Length; j < setCount; ++j)
			{
				LookSet set = _sets[j];

				for (int i = 0, nodeCount = set.Nodes.Length; i < nodeCount; ++i)
				{
					ClipNode node = set.Nodes[i];
					node.CreatePlayable(Controller.Graph);

					_mixer.AddInput(node.PlayableClip, 0);
					++_inputs;
				}
			}

			AddPlayable(_mixer, 0);
		}

		protected override void OnInitialize()
		{
			_kcc     = Controller.GetComponentNoAlloc<KCC>();
			_weapons = Controller.GetComponentNoAlloc<Weapons>();
			_jetpack = Controller.GetComponentNoAlloc<Jetpack>();
		}

		protected override void OnDespawned()
		{
			if (_mixer.IsValid() == true)
			{
				_mixer.Destroy();
			}

			for (int j = 0, setCount = _sets.Length; j < setCount; ++j)
			{
				LookSet set = _sets[j];

				for (int i = 0, nodeCount = set.Nodes.Length; i < nodeCount; ++i)
				{
					ClipNode node = set.Nodes[i];
					node.DestroyPlayable();
				}
			}
		}

		protected override void OnFixedUpdate()
		{
			Refresh(_kcc.FixedData.LookPitch);
		}

		protected override void OnInterpolate()
		{
			Refresh(_kcc.RenderData.LookPitch);
		}

		// IAnimationConvertor INTERFACE

		void IAnimationConvertor.Convert(AnimationConvertor convertor)
		{
			for (int i = 0; i < _sets.Length; ++i)
			{
				_sets[i].Convert(convertor);
			}
		}

		// PRIVATE METHODS

		private void Refresh(float pitch)
		{
			int setID = GetSetID();
			if (setID < 0)
				return;

			for (int i = 0, count = _inputs; i < count; ++i)
			{
				_mixer.SetInputWeight(i, 0.0f);
			}

			int   node  = 0;
			float angle = (pitch + 720.0f + _sets[setID].Offset) % 360.0f;

			if (angle > 180.0f)
			{
				node  = 1;
				angle = 360.0f - angle;
			}

			angle = Mathf.Clamp(Mathf.Pow(angle, _sets[setID].Power), 0.0f, 90.0f);

			if (angle.IsNaN() == true)
			{
				angle = 0.0f;
			}

			int clipIndex = setID * 2 + node;

			_sets[setID].Nodes[node].PlayableClip.SetTime(angle / 90.0f);

			_mixer.SetInputWeight(clipIndex, 1.0f);
		}

		private int GetSetID()
		{
			if (_jetpack.IsActive == true)
				return -1;

			int currentWeaponSlot = _weapons.CurrentWeaponSlot;
			if (currentWeaponSlot > 2)
			{
				currentWeaponSlot = 1; // For grenades we use pistol set
			}

			if (currentWeaponSlot < 0)
				return -1;

			return currentWeaponSlot;
		}

		[Serializable]
		private sealed class LookSet
		{
			public ClipNode[] Nodes;
			public float      Offset;
			public float      Power = 1.0f;

			public void Convert(AnimationConvertor convertor)
			{
				for (int i = 0; i < Nodes.Length; ++i)
				{
					Nodes[i].Convert(convertor);
				}
			}
		}
	}
}
