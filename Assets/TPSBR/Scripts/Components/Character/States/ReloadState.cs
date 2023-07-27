using Fusion.KCC;

namespace TPSBR
{
	using Fusion.Animations;

	public sealed class ReloadState : MultiClipState
	{
		// PRIVATE MEMBERS

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
