using Fusion.KCC;

namespace TPSBR
{
	using UnityEngine;
	using Fusion.Animations;

	public sealed class UnequipState : MultiClipState
	{
		// PUBLIC MEMBERS

		public float DisarmTime => _disarmTime;
		public float SwitchTime => _switchTime;

		// PRIVATE MEMBERS

		[SerializeField]
		private float _disarmTime = 0.5f;
		[SerializeField]
		private float _switchTime = 1.0f;

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

			return currentWeaponSlot;
		}

		// AnimationState INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_kcc     = Controller.GetComponentNoAlloc<KCC>();
			_weapons = Controller.GetComponentNoAlloc<Weapons>();
		}
	}
}
