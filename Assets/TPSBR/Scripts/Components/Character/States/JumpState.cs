using Fusion.Animations;

namespace TPSBR
{
	using UnityEngine;

	public class JumpState : MultiMirrorBlendTreeState
	{
		// PRIVATE MEMBERS

		private Weapons _weapons;

		// MultiMirrorBlendTreeState INTERFACE

		protected override int GetSetID()
		{
			if (_weapons.CurrentWeaponSlot > 2)
				return 1; // For grenades we use pistol set

			return Mathf.Max(0, _weapons.CurrentWeaponSlot);
		}

		// AnimationState INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_weapons = Controller.GetComponentNoAlloc<Weapons>();
		}
	}
}
