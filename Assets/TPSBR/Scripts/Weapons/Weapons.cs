using Fusion;

namespace TPSBR
{
	using System;
	using UnityEngine;

	[Serializable]
	public sealed class WeaponSlot
	{
		public Transform  Active;
		public Transform  Inactive;
		[NonSerialized]
		public Quaternion BaseRotation;
	}

	public class Weapons : NetworkBehaviour, IBeforeTick
	{
		// PUBLIC MEMBERS

		public Weapon       CurrentWeapon           => _weapons[CurrentWeaponSlot];
		public Weapon       PendingWeapon           => _weapons[PendingWeaponSlot];
		public IInteraction InteractionTarget       => _interactionTarget;
		public float        WeaponDropTime          => _weaponDropTime;
		public Transform    WeaponHandle            => _slots[CurrentWeaponSlot].Active;
		public Quaternion   WeaponBaseRotation      => _slots[CurrentWeaponSlot].BaseRotation;

		public Vector3      TargetPoint             { get; private set; }
		public bool         UndesiredTargetPoint    { get; private set; }

		[Networked, HideInInspector]
		public int          CurrentWeaponSlot       { get; private set; }
		[Networked(OnChanged = nameof(OnPendingWeaponChanged), OnChangedTargets = OnChangedTargets.All), HideInInspector]
		public int          PendingWeaponSlot       { get; private set; }
		[Networked, HideInInspector]
		public int          PreviousWeaponSlot      { get; private set; }

		[Networked, HideInInspector]
		public TickTimer    DropWeaponTimer         { get; private set; }

		public event Action<string> InteractionFailed;

		// PRIVATE MEMBERS

		[SerializeField]
		private LayerMask _hitMask;

		[SerializeField]
		private Weapon[] _initialWeapons;
		[SerializeField]
		private WeaponSlot[] _slots;

		[SerializeField]
		private Vector3 _dropWeaponImpulse = new Vector3(5, 5f, 10f);

		[Header("Audio")]
		[SerializeField]
		private Transform _fireAudioEffectsRoot;
		[SerializeField]
		private AudioSetup _weaponSwitchSound;

		[Header("Interactions")]
		[SerializeField]
		private LayerMask _interactionMask;
		[SerializeField]
		private float _interactionDistance = 2f;
		[SerializeField]
		private float _interactionPrecisionRadius = 0.3f;
		[SerializeField]
		private float _weaponDropTime;

		[Networked(OnChanged = nameof(OnWeaponsChanged), OnChangedTargets = OnChangedTargets.All), Capacity(8)]
		private NetworkArray<Weapon> _weapons { get; }

		private bool _forceWeaponsRefresh;

		private Agent _agent;

		private IInteraction _interactionTarget;
		private RaycastHit[] _interactionHits = new RaycastHit[10];

		private AudioEffect[] _fireAudioEffects;

		// PUBLIC METHODS

		public void DisarmCurrentWeapon()
		{
			CurrentWeaponSlot = 0;
		}

		public void ArmPendingWeapon()
		{
			CurrentWeaponSlot = PendingWeaponSlot;
		}

		public void OnSpawned()
		{
			if (Object.HasStateAuthority == false)
				return;

			int bestWeaponSlot = 0;

			// Spawn initial weapons
			for (int i = 0; i < _initialWeapons.Length; i++)
			{
				var weaponPrefab = _initialWeapons[i];
				if (weaponPrefab == null)
					continue;

				var weapon = Runner.Spawn(weaponPrefab, inputAuthority: Object.InputAuthority);
				AddWeapon(weapon);

				if (bestWeaponSlot == 0 || weapon.WeaponSlot < 3)
				{
					bestWeaponSlot = weapon.WeaponSlot;
				}
			}

			// Equip best weapon
			SetPendingWeapon(bestWeaponSlot);
		}

		public void OnDespawned()
		{
			// Cleanup weapons
			for (int i = 0; i < _weapons.Length; i++)
			{
				if (_weapons[i] != null)
				{
					Runner.Despawn(_weapons[i].Object);
					_weapons.Set(i, null);
				}
			}

			InteractionFailed = null;
		}

		public void OnFixedUpdate()
		{
			if (Object.HasStateAuthority == false)
				return;

			if (_agent.Health.IsAlive == false)
			{
				DropAllWeapons();
				return;
			}

			// Autoswitch to valid weapon if current is invalid
			if (CurrentWeapon != null && CurrentWeaponSlot == PendingWeaponSlot && CurrentWeapon.ValidOnlyWithAmmo == true && CurrentWeapon.HasAmmo() == false)
			{
				if (PreviousWeaponSlot == 0)
				{
					// Immediately hide weapon, instead of unequipping it
					CurrentWeaponSlot = 0;
				}

				SetPendingWeapon(PreviousWeaponSlot, false);
			}
		}

		public void OnLateFixedUpdate()
		{
			RefreshWeapons();
		}

		public void OnRender()
		{
			if (_agent.IsLocal == false)
				return;

			if (_agent.Health.IsAlive == false)
			{
				_interactionTarget = null;
				return;
			}

			UpdateInteractionTarget();
			TargetPoint = GetTargetPoint(true);
		}

		public bool IsSwitchingWeapon()
		{
			return PendingWeaponSlot != CurrentWeaponSlot;
		}

		public bool CanFireWeapon(bool keyDown)
		{
			return IsSwitchingWeapon() == false && CurrentWeapon != null && CurrentWeapon.CanFire(keyDown) == true;
		}

		public bool CanReloadWeapon(bool autoReload)
		{
			return IsSwitchingWeapon() == false && CurrentWeapon != null && CurrentWeapon.CanReload(autoReload) == true;
		}

		public bool CanAim()
		{
			return IsSwitchingWeapon() == false && CurrentWeapon != null && CurrentWeapon.CanAim() == true;
		}

		public Vector2 GetRecoil()
		{
			var firearmWeapon = CurrentWeapon as FirearmWeapon;
			var recoil = firearmWeapon != null ? firearmWeapon.Recoil : Vector2.zero;
			return new Vector2(-recoil.y, recoil.x); // Convert to axis angles
		}

		public void SetRecoil(Vector2 axisRecoil)
		{
			var firearmWeapon = CurrentWeapon as FirearmWeapon;

			if (firearmWeapon == null)
				return;

			firearmWeapon.Recoil = new Vector2(axisRecoil.y, -axisRecoil.x);
		}

		public void TryInteract(bool interact, bool hold)
		{
			if (hold == false)
			{
				DropWeaponTimer = default;
				return;
			}

			if (IsSwitchingWeapon() == true)
			{
				DropWeaponTimer = default;
				return;
			}

			if (CurrentWeapon != null && CurrentWeapon.IsBusy() == true)
			{
				DropWeaponTimer = default;
				return;
			}

			if (Object.HasStateAuthority == false)
				return;

			UpdateInteractionTarget();

			if (_interactionTarget == null)
			{
				if (DropWeaponTimer.IsRunning == false && CurrentWeaponSlot > 0 && interact == true)
				{
					DropWeaponTimer = TickTimer.CreateFromSeconds(Runner, _weaponDropTime);
				}

				if (DropWeaponTimer.Expired(Runner) == true)
				{
					DropWeapon(CurrentWeaponSlot);
					DropWeaponTimer = default;
				}

				return;
			}

			if (interact == false)
				return;

			if (_interactionTarget is DynamicPickup dynamicPickup && dynamicPickup.Provider is Weapon pickupWeapon)
			{
				var ownedWeapon = _weapons[pickupWeapon.WeaponSlot];
				if (ownedWeapon != null && ownedWeapon.WeaponID == pickupWeapon.WeaponID)
				{
					// We already have this weapon, try add at least the ammo
					var firearmWeapon = pickupWeapon as FirearmWeapon;
					bool consumed = firearmWeapon != null && ownedWeapon.AddAmmo(firearmWeapon.TotalAmmo);

					if (consumed == true)
					{
						dynamicPickup.UnassignObject();
						Runner.Despawn(pickupWeapon.Object);
					}
				}
				else
				{
					dynamicPickup.UnassignObject();
					PickupWeapon(pickupWeapon);
				}
			}
			else if (_interactionTarget is WeaponPickup weaponPickup)
			{
				if (weaponPickup.Consumed == true || weaponPickup.IsDisabled == true)
					return;

				var ownedWeapon = _weapons[weaponPickup.WeaponPrefab.WeaponSlot];
				if (ownedWeapon != null && ownedWeapon.WeaponID == weaponPickup.WeaponPrefab.WeaponID)
				{
					// We already have this weapon, try add at least the ammo
					var firearmWeapon = weaponPickup.WeaponPrefab as FirearmWeapon;
					bool consumed = firearmWeapon != null && ownedWeapon.AddAmmo(firearmWeapon.InitialAmmo);

					if (consumed == true)
					{
						weaponPickup.TryConsume(_agent, out string weaponPickupResult);
					}
				}
				else
				{
					weaponPickup.TryConsume(_agent, out string weaponPickupResult2);

					var weapon = Runner.Spawn(weaponPickup.WeaponPrefab, inputAuthority: Object.InputAuthority);
					PickupWeapon(weapon);
				}
			}
			else if (_interactionTarget is ItemBox itemBox)
			{
				itemBox.Open();
			}
			else if (_interactionTarget is StaticPickup staticPickup)
			{
				bool success = staticPickup.TryConsume(_agent, out string result);
				if (success == false && result.HasValue() == true)
				{
					RPC_InteractionFailed(result);
				}
			}
		}

		public bool ToggleWeapon(int weaponSlot)
		{
			if (weaponSlot == PendingWeaponSlot)
			{
				SetPendingWeapon(0);
				return true;
			}

			if (_weapons[weaponSlot] == null)
				return false;

			SetPendingWeapon(weaponSlot);
			return true;
		}

		public bool SwitchWeapon(int weaponSlot)
		{
			if (weaponSlot == PendingWeaponSlot)
				return false;

			var weapon = _weapons[weaponSlot];
			if (weapon == null || (weapon.ValidOnlyWithAmmo == true && weapon.HasAmmo() == false))
				return false;

			SetPendingWeapon(weaponSlot);
			return true;
		}

		public bool HasWeapon(int slot, bool checkAmmo = false)
		{
			if (slot < 0 || slot >= _weapons.Length)
				return false;

			var weapon = _weapons[slot];
			return weapon != null && (checkAmmo == false || (weapon.Object != null && weapon.HasAmmo() == true));
		}

		public Weapon GetWeapon(int slot)
		{
			return _weapons[slot];
		}

		public int GetNextWeaponSlot(int fromSlot, int minSlot = 0, bool checkAmmo = true)
		{
			int weaponCount = _weapons.Length;

			for (int i = 0; i < weaponCount; i++)
			{
				int slot = (i + fromSlot + 1) % weaponCount;

				if (slot < minSlot)
					continue;

				var weapon = _weapons[slot];

				if (weapon == null)
					continue;

				if (checkAmmo == true && weapon.HasAmmo() == false)
					continue;

				return slot;
			}

			return 0;
		}

		public bool Fire()
		{
			if (CurrentWeapon == null)
				return false;

			var targetPoint = GetTargetPoint(false);
			var fireTransform = _agent.Character.GetFireTransform();

			CurrentWeapon.Fire(fireTransform.Position, targetPoint, _hitMask);

			return true;
		}

		public bool Reload()
		{
			if (CurrentWeapon == null)
				return false;

			CurrentWeapon.Reload();
			return true;
		}

		public bool AddAmmo(int weaponSlot, int amount, out string result)
		{
			if (weaponSlot < 0 || weaponSlot >= _weapons.Length)
			{
				result = string.Empty;
				return false;
			}

			var weapon = _weapons[weaponSlot];
			if (weapon == null)
			{
				result = "No weapon with this type of ammo";
				return false;
			}

			bool ammoAdded = weapon.AddAmmo(amount);
			result = ammoAdded == true ? string.Empty : "Cannot add more ammo";

			return ammoAdded;
		}

		// IBeforeTick INTERFACE

		void IBeforeTick.BeforeTick()
		{
			RefreshWeapons();
		}

		// MONOBEHAVIOUR

		protected void Awake()
		{
			_agent = GetComponent<Agent>();
			_fireAudioEffects = _fireAudioEffectsRoot.GetComponentsInChildren<AudioEffect>();

			foreach (WeaponSlot slot in _slots)
			{
				if (slot.Active != null)
				{
					slot.BaseRotation = slot.Active.localRotation;
				}
			}
		}

		// PRIVATE METHODS

		private void RefreshWeapons()
		{
			var currentWeapon = CurrentWeapon;

			if (currentWeapon == null || currentWeapon.Object == null)
				return;

			if (_forceWeaponsRefresh == false && currentWeapon.IsArmed == true && currentWeapon.NeedsParentRefresh == false)
				return; // Proper weapon is ready

			Vector2 lastRecoil = Vector2.zero;

			for (int i = 0; i < _weapons.Length; i++)
			{
				var weapon = _weapons[i];

				if (weapon == null)
					continue;

				if (weapon.IsArmed == true)
				{
					if (weapon != currentWeapon)
					{
						weapon.DisarmWeapon();
					}

					if (weapon is FirearmWeapon firearmWeapon)
					{
						lastRecoil = firearmWeapon.Recoil;
					}
				}

				weapon.SetParent(_slots[weapon.WeaponSlot].Inactive);
			}

			currentWeapon.ArmWeapon();
			currentWeapon.SetParent(_slots[currentWeapon.WeaponSlot].Active);
			currentWeapon.AssignFireAudioEffects(_fireAudioEffectsRoot, _fireAudioEffects);

			if (currentWeapon is FirearmWeapon newFirearmWeapon)
			{
				// Recoil transfers to new weapon
				// (might be better to have recoil as an agent property instead of a weapon property)
				newFirearmWeapon.Recoil = lastRecoil;
			}

			_forceWeaponsRefresh = false;
		}

		private void DropAllWeapons()
		{
			for (int i = 1; i < _weapons.Length; i++)
			{
				DropWeapon(i);
			}
		}

		private void DropWeapon(int weaponSlot)
		{
			if (weaponSlot <= 0)
				return;

			var weapon = _weapons[weaponSlot];

			if (weapon == null)
				return;

			if (weapon.PickupPrefab == null)
			{
				Debug.LogWarning($"Cannot drop weapon {gameObject.name}, pickup prefab not assigned.");
				return;
			}

			weapon.DisarmWeapon();

			if (weaponSlot == CurrentWeaponSlot)
			{
				if (PreviousWeaponSlot == 0)
				{
					// Immediately hide weapon, instead of unequipping it
					CurrentWeaponSlot = 0;
				}

				SetPendingWeapon(PreviousWeaponSlot, false);
			}

			var weaponTransform = weapon.transform;

			var pickup = Runner.Spawn(weapon.PickupPrefab, weaponTransform.position, weaponTransform.rotation,
				PlayerRef.None, BeforePickupSpawned);

			RemoveWeapon(weaponSlot);

			var pickupRigidbody = pickup.GetComponent<Rigidbody>();
			if (pickupRigidbody != null)
			{
				var forcePosition = weaponTransform.TransformPoint(new Vector3(-0.005f, 0.005f, 0.015f) * weaponSlot);
				pickupRigidbody.AddForceAtPosition(weaponTransform.rotation * _dropWeaponImpulse, forcePosition, ForceMode.Impulse);
			}

			void BeforePickupSpawned(NetworkRunner runner, NetworkObject obj)
			{
				var dynamicPickup = obj.GetComponent<DynamicPickup>();
				dynamicPickup.AssignObject(_weapons[weaponSlot].Object.Id);
			}
		}

		private void PickupWeapon(Weapon weapon)
		{
			if (weapon == null)
				return;

			DropWeapon(weapon.WeaponSlot);
			AddWeapon(weapon);

			if (weapon.WeaponSlot >= CurrentWeaponSlot && weapon.WeaponSlot < 5)
			{
				// Switch only to better weapon
				CurrentWeaponSlot = weapon.WeaponSlot;
				SetPendingWeapon(weapon.WeaponSlot);
			}
		}

		private void UpdateInteractionTarget()
		{
			_interactionTarget = null;

			var cameraTransform = _agent.Character.GetCameraTransform();
			var cameraDirection = cameraTransform.Rotation * Vector3.forward;

			var physicsScene = Runner.GetPhysicsScene();
			int hitCount = physicsScene.SphereCast(cameraTransform.Position, _interactionPrecisionRadius, cameraDirection, _interactionHits, _interactionDistance, _interactionMask, QueryTriggerInteraction.Ignore);

			if (hitCount == 0)
				return;

			RaycastHit validHit = default;

			// Try to pick object that is directly in the center of the crosshair
			if (physicsScene.Raycast(cameraTransform.Position, cameraDirection, out RaycastHit raycastHit, _interactionDistance, _interactionMask, QueryTriggerInteraction.Ignore) == true && raycastHit.collider.gameObject.layer == ObjectLayer.Interaction)
			{
				validHit = raycastHit;
			}
			else
			{
				RaycastUtility.Sort(_interactionHits, hitCount);

				for (int i = 0; i < hitCount; i++)
				{
					var hit = _interactionHits[i];

					if (hit.collider.gameObject.layer == ObjectLayer.Default)
						return; // Something is blocking interaction

					if (hit.collider.gameObject.layer == ObjectLayer.Interaction)
					{
						validHit = hit;
						break;
					}
				}
			}

			var collider = validHit.collider;

			if (collider == null)
				return;

			var interaction = collider.GetComponent<IInteraction>();
			if (interaction == null)
			{
				interaction = collider.GetComponentInParent<IInteraction>();
			}

			if (interaction != null && interaction.IsActive == true)
			{
				_interactionTarget = interaction;
			}
		}

		private Vector3 GetTargetPoint(bool checkReachability)
		{
			var cameraTransform = _agent.Character.GetCameraTransform();
			var cameraDirection = cameraTransform.Rotation * Vector3.forward;

			var fireTransform = _agent.Character.GetFireTransform();
			var targetPoint = cameraTransform.Position + cameraDirection * 500f;

			if (Runner.LagCompensation.Raycast(cameraTransform.Position, cameraDirection, 500f, Object.InputAuthority,
				out LagCompensatedHit hit, _hitMask, HitOptions.IncludePhysX | HitOptions.SubtickAccuracy | HitOptions.IgnoreInputAuthority) == true)
			{
				var firingDirection = (hit.Point - fireTransform.Position).normalized;

				// Check angle
				if (Vector3.Dot(cameraDirection, firingDirection) > 0.95f)
				{
					targetPoint = hit.Point;
				}
			}

			if (checkReachability == true)
			{
				UndesiredTargetPoint = CurrentWeapon != null && CurrentWeapon.CanFireToPosition(fireTransform.Position, ref targetPoint, _hitMask) == false;
			}

			return targetPoint;
		}

		private void SetPendingWeapon(int slot, bool currentPendingWeaponValid = true)
		{
			if (slot == PendingWeaponSlot)
				return;

			PreviousWeaponSlot = currentPendingWeaponValid == true ? PendingWeaponSlot : 0;
			PendingWeaponSlot = slot;

			//Debug.Log($"Current {CurrentWeaponSlot}, Pending {PendingWeaponSlot}, Previous {PreviousWeaponSlot}");
		}

		private void AddWeapon(Weapon weapon)
		{
			if (weapon == null)
				return;

			RemoveWeapon(weapon.WeaponSlot);

			weapon.Owner = _agent;
			weapon.Object.AssignInputAuthority(Object.InputAuthority);

			var aoiProxy = weapon.GetComponent<NetworkAreaOfInterestProxy>();
			aoiProxy.SetPositionSource(transform);

			Runner.SetPlayerAlwaysInterested(Object.InputAuthority, weapon.Object, true);

			_weapons.Set(weapon.WeaponSlot, weapon);
		}

		private void RemoveWeapon(int slot)
		{
			var weapon = _weapons[slot];
			if (weapon == null)
				return;

			weapon.Owner = null;
			weapon.Object.RemoveInputAuthority();

			var aoiProxy = weapon.GetComponent<NetworkAreaOfInterestProxy>();
			aoiProxy.ResetPositionSource();

			Runner.SetPlayerAlwaysInterested(Object.InputAuthority, weapon.Object, false);

			_weapons.Set(slot, null);
		}

		// NETWORK CALLBACKS

		public static void OnPendingWeaponChanged(Changed<Weapons> changed)
		{
			changed.Behaviour._agent.Effects.PlaySound(changed.Behaviour._weaponSwitchSound);
		}

		public static void OnWeaponsChanged(Changed<Weapons> changed)
		{
			changed.Behaviour._forceWeaponsRefresh = true;
		}

		// RPCs

		[Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
		private void RPC_InteractionFailed(string reason)
		{
			InteractionFailed?.Invoke(reason);
		}
	}
}
