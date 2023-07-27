using Fusion.Animations;

namespace TPSBR
{
	using Fusion.KCC;

	public sealed class EquipState : MultiClipState
	{
		// PRIVATE MEMBERS

		private KCC     _kcc;
		private Weapons _weapons;

		// MultiClipState INTERFACE

		protected override int GetClipID()
		{
			int pendingWeaponSlot = _weapons.PendingWeaponSlot;
			if (pendingWeaponSlot > 2)
			{
				pendingWeaponSlot = 1; // For grenades we use pistol set
			}

			if (pendingWeaponSlot < 0)
				return 0;

			return pendingWeaponSlot;
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
