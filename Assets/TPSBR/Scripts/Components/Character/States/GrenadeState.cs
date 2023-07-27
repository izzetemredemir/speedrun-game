using UnityEngine;

namespace TPSBR
{
	using Fusion.Animations;

	public class GrenadeState : MixerState
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private ClipState _holdState;
		[SerializeField]
		private ClipState _armState;
		[SerializeField]
		private ClipState _throwState;
		[SerializeField]
		private ClipState _reloadState;
		[SerializeField]
		private ClipState _equipState;

		private Weapons _weapons;

		// PUBLIC METHODS

		public void ProcessThrow(bool start, bool hold)
		{
			AnimationState activeState = GetActiveState();

			if (activeState == _throwState)
			{
				float time = _throwState.AnimationTime;

				if (time > 0.45f)
				{
					// Fire half way in throw
					_weapons.Fire();
				}

				if (time < 0.95f)
					return;
			}

			if (activeState == _equipState && _equipState.IsFinished(0.8f) == false)
				return; // Wait for equip to finish

			if (activeState == _reloadState && _reloadState.IsFinished(0.8f) == false)
				return; // Wait for reload to finish

			if (activeState == _armState && hold == false && _weapons.CanFireWeapon(start) == true)
			{
				_throwState.Activate(0.15f);
			}
			else if ((activeState == _holdState || activeState == _reloadState) && (start == true || hold == true) && _weapons.CanFireWeapon(start) == true)
			{
				(_weapons.CurrentWeapon as ThrowableWeapon).ArmProjectile();
				_armState.Activate(0.25f);
			}
			else if (activeState != _holdState && start == false && hold == false)
			{
				_holdState.Activate(0.25f);
			}
		}

		public bool ProcessReload()
		{
			AnimationState activeState = GetActiveState();

			if (activeState == _throwState && _throwState.IsFinished(0.9f) == false)
				return false;

			// Start reload
			_reloadState.Activate(0.2f);
			return true;
		}

		public bool CanSwitchWeapon()
		{
			AnimationState activeState = GetActiveState();
			return activeState != _throwState;
		}

		public void Equip()
		{
			_equipState.Activate(0.2f);
		}

		// MixerState INTERFACE

		protected override void OnInitialize()
		{
			_weapons = GetComponentInParent<Weapons>();
		}

		protected override void OnFixedUpdate()
		{
			AnimationState activeState = GetActiveState();

			if (activeState == _equipState && _equipState.AnimationTime > 0.5f)
			{
				_weapons.ArmPendingWeapon();
			}

			if (activeState == _throwState && _throwState.AnimationTime > 0.95f)
			{
				_holdState.Activate(0.25f);
			}

			if (_weapons.PendingWeapon is ThrowableWeapon == false)
			{
				Deactivate(0.2f);
			}
		}
	}
}
