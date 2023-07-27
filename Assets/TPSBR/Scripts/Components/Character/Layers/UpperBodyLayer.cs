using Fusion.Animations;

namespace TPSBR
{
	using UnityEngine;

	public sealed class UpperBodyLayer : AnimationLayer
	{
		// PUBLIC MEMBERS

		public ReloadState  Reload  => _reload;
		public EquipState   Equip   => _equip;
		public UnequipState Unequip => _unequip;
		public GrenadeState Grenade => _grenade;

		// PRIVATE MEMBERS

		[SerializeField]
		private ReloadState  _reload;
		[SerializeField]
		private EquipState   _equip;
		[SerializeField]
		private UnequipState _unequip;
		[SerializeField]
		private GrenadeState _grenade;

		private Weapons      _weapons;

		// AnimationLayer INTERFACE

		protected override void OnInitialize()
		{
			_weapons = Controller.GetComponentNoAlloc<Weapons>();
		}

		protected override void OnFixedUpdate()
		{
			if (_reload.IsFinished(1.0f) == true)
			{
				_reload.Deactivate(0.2f);
			}

			float disarmTime = _unequip.DisarmTime;
			float switchTime = _unequip.SwitchTime;
			if (_weapons.PendingWeaponSlot > 0 && switchTime < disarmTime)
			{
				disarmTime = _unequip.SwitchTime;
			}

			if (_unequip.IsFinished(disarmTime) == true)
			{
				_weapons.DisarmCurrentWeapon();

				if (_weapons.PendingWeaponSlot > 0)
				{
					if (_unequip.IsFinished(switchTime) == true)
					{
						_equip.Activate(0.2f);
						_equip.AnimationTime = 0.0f;
					}
				}
				else
				{
					_unequip.Deactivate(0.2f);
				}
			}

			float equipTime = 0.4f;

			if (_equip.IsFinished(equipTime) == true)
			{
				_weapons.ArmPendingWeapon();

				if (_equip.IsFinished(1.0f) == true)
				{
					_equip.Deactivate(0.2f);
				}
			}
		}
	}
}
