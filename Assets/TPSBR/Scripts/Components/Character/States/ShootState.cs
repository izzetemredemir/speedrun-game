using Fusion.KCC;

namespace TPSBR
{
	using UnityEngine;
	using UnityEngine.Playables;
	using Fusion.Animations;

	public sealed class ShootState : MultiClipState
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private float _animationPower = 1.0f;

		private KCC     _kcc;
		private Weapons _weapons;

		// MultiClipState INTERFACE

		protected override int GetClipID()
		{
			int currentWeaponSlot = _weapons.CurrentWeaponSlot;
			if (currentWeaponSlot > 2)
			{
				currentWeaponSlot = 1; // For grenades we use pistol set
			}

			if (currentWeaponSlot < 0)
				return 0;

			return currentWeaponSlot * 2 + 1;
		}

		// AnimationState INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_kcc     = Controller.GetComponentNoAlloc<KCC>();
			_weapons = Controller.GetComponentNoAlloc<Weapons>();
		}

		protected override void OnFixedUpdate()
		{
			base.OnFixedUpdate();

			int clipID = GetClipID();
			int idleID = clipID - 1;

			Mixer.SetInputWeight(idleID, 1.0f - _animationPower);
			Mixer.SetInputWeight(clipID, _animationPower);

			Nodes[idleID].PlayableClip.SetTime(AnimationTime);
		}

		protected override void OnInterpolate()
		{
			base.OnInterpolate();

			int clipID = GetClipID();
			int idleID = clipID - 1;

			Mixer.SetInputWeight(idleID, 1.0f - _animationPower);
			Mixer.SetInputWeight(clipID, _animationPower);

			Nodes[idleID].PlayableClip.SetTime(InterpolatedAnimationTime);
		}
	}
}
