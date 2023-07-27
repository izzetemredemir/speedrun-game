using UnityEngine;
using Fusion;
using System.Collections.Generic;

namespace TPSBR
{
	public abstract class FirearmWeapon : Weapon, IDynamicPickupProvider
	{
		// PUBLIC MEMBERS

		public bool      IsFiring          { get { return _state.IsBitSet(0); } set { _state = _state.SetBitNoRef(0, value); } }
		public bool      IsReloading       { get { return _state.IsBitSet(1); } set { _state = _state.SetBitNoRef(1, value); } }

		public int       MagazineAmmo      => _magazineAmmo;
		public int       MaxMagazineAmmo   => _maxMagazineAmmo;
		public int       WeaponAmmo        => _weaponAmmo;
		public int       TotalAmmo         => _magazineAmmo + _weaponAmmo;
		public int       InitialAmmo       => _initialAmmo;
		public float     ReloadTime        => _reloadTime;
		public Transform FireTransform     => _fireTransform;

		public float     Cooldown          => _cooldown.ExpiredOrNotRunning(Runner) == false ? _cooldown.RemainingTime(Runner).Value : 0f;

		public float     TotalDispersion   => GetTotalDispersion();

		[Networked, HideInInspector]
		public Vector2   Recoil            { get; set; }

		// PRIVATE MEMBERS

		[Header("General")]
		[SerializeField]
		private int           _cadence = 600;
		[SerializeField]
		protected int         _initialAmmo = 150;
		[SerializeField]
		private int           _maxMagazineAmmo = 30;
		[SerializeField]
		private int           _maxWeaponAmmo = 120;
		[SerializeField]
		private bool          _hasUnlimitedAmmo;
		[SerializeField]
		private float         _reloadTime = 2f;
		[SerializeField]
		private bool          _fireOnKeyDownOnly;
		[SerializeField]
		private int           _projectilesPerShot = 1;
		[SerializeField]
		private ShakeSetup    _cameraShakePosition;
		[SerializeField]
		private ShakeSetup    _cameraShakeRotation;
		[SerializeField]
		private bool          _supressReloadWhileAimed;

		[Header("Dispersion")]
		[SerializeField]
		private float         _minDispersion = 0.3f;
		[SerializeField]
		private float         _minFireDispersion = 1f;
		[SerializeField]
		private float         _maxFireDispersion = 7f;
		[SerializeField]
		private float         _maxDispersion = 15f;
		[SerializeField]
		private float         _dispersionIncreasePerShot = 0.5f;
		[SerializeField]
		private float         _dispersionDecreaseRate = 10f;

		[Header("Recoil")]
		[SerializeField]
		private RecoilPattern _recoilPattern;
		[SerializeField]
		private float         _recoilAimMultiplier = 1f;
		[SerializeField]
		private float         _recoilSpeedMultiplier = 1f;
		[SerializeField]
		private float         _recoilDecreaseSpeed = 20f;

		[Header("Audio")]
		[SerializeField]
		private Transform     _audioEffectsTransform;
		[SerializeField]
		private AudioEffect   _localAudio;
		[SerializeField]
		private AudioSetup    _fireSound;
		[SerializeField]
		private AudioSetup    _reloadSound;
		[SerializeField]
		private AudioSetup    _readySound;

		[Header("Fire")]
		[SerializeField]
		private Transform     _fireTransform;
		[SerializeField]
		private GameObject    _fireParticlePlayer;
		[SerializeField]
		private GameObject    _fireParticleProxy;

		[Networked]
		private byte          _state { get; set; }
		[Networked(OnChanged = nameof(OnStateChanged), OnChangedTargets = OnChangedTargets.All)]
		private TickTimer _cooldown { get; set; }
		[Networked, HideInInspector]
		protected int         _projectilesCount { get; set; }
		[Networked]
		private int           _magazineAmmo { get; set; }
		[Networked]
		private int           _weaponAmmo { get; set; }

		[Networked]
		private float         _dispersion { get; set; }

		[Networked]
		private int           _recoilStartShot { get; set; }

		private int           _lastVisibleProjectileCount;
		private bool          _proxyWasOutsideOfAOI;
		private int           _fireTicks;
		private int           _recoilTicks;

		private List<LagCompensatedHit> _validHits = new List<LagCompensatedHit>(16);

		// Weapon INTERFACE

		public override void Spawned()
		{
			base.Spawned();

			_magazineAmmo = Mathf.Clamp(_initialAmmo, 0, _maxMagazineAmmo);
			_weaponAmmo = Mathf.Clamp(_initialAmmo - _magazineAmmo, 0, _maxWeaponAmmo);

			_dispersion = _minFireDispersion;

			float fireTime = 60f / _cadence;
			_fireTicks = (int)System.Math.Ceiling(fireTime / (double)Runner.DeltaTime);
			_recoilTicks = Mathf.Min(_fireTicks, (int)(_fireTicks / _recoilSpeedMultiplier));

			_lastVisibleProjectileCount = _projectilesCount;
			_proxyWasOutsideOfAOI = false;
		}

		public override void FixedUpdateNetwork()
		{
			base.FixedUpdateNetwork();

			if (Object.IsProxy == true)
			{
				if (Object.LastReceiveTick < Runner.Simulation.Tick - 2.0f * Runner.Config.Simulation.TickRate)
				{
					_proxyWasOutsideOfAOI = true;
				}

				return;
			}

			bool recoilInProgress = IsFiring == true && _recoilPattern != null && _cooldown.RemainingTicks(Runner) >= (_fireTicks - _recoilTicks);
			if (recoilInProgress == true)
			{
				float aimMultiplier = Owner != null && Owner.Character.CharacterController.Data.Aim == true ? _recoilAimMultiplier : 1.0f;

				Vector2 shotRecoil = _recoilPattern.GetRecoil(_projectilesCount - _recoilStartShot) * aimMultiplier;
				Recoil += shotRecoil / _recoilTicks;
			}
			else if (Recoil != Vector2.zero)
			{
				Recoil = Vector2.Lerp(Recoil, Vector2.zero, Runner.DeltaTime * _recoilDecreaseSpeed);

				float recoilSqrMagnitude = Recoil.sqrMagnitude;

				if (recoilSqrMagnitude < 0.05f)
				{
					_recoilStartShot = _projectilesCount + 1;
				}

				if (recoilSqrMagnitude < 0.005f)
				{
					Recoil = Vector2.zero;
				}
			}

			if (IsFiring == false)
			{
				_dispersion = Mathf.Clamp(_dispersion - Runner.DeltaTime * _dispersionDecreaseRate, _minFireDispersion, _maxDispersion);
			}

			int? cooldownTargetTick = _cooldown.TargetTick;
			if (cooldownTargetTick.HasValue == true && cooldownTargetTick.Value <= Runner.Simulation.Tick)
			{
				if (IsReloading == true)
				{
					int reloadAmmo = _maxMagazineAmmo - _magazineAmmo;

					if (_hasUnlimitedAmmo == false)
					{
						reloadAmmo = Mathf.Min(reloadAmmo, _weaponAmmo);
						_weaponAmmo -= reloadAmmo;
					}

					_magazineAmmo += reloadAmmo;
				}

				IsFiring = false;
				IsReloading = false;
			}
		}

		public override void Render()
		{
			base.Render();

			if (Runner.Mode != SimulationModes.Server)
			{
				UpdateVisibleProjectiles();
			}
		}

		public override bool IsBusy()
		{
			return IsFiring || IsReloading;
		}

		public override bool CanFire(bool keyDown)
		{
			if (IsFiring == true || IsReloading == true)
				return false;

			if (_magazineAmmo <= 0)
				return false;

			if (_fireOnKeyDownOnly == true && keyDown == false)
				return false;

			return _cooldown.ExpiredOrNotRunning(Runner);
		}

		public override bool CanReload(bool autoReload)
		{
			if (IsFiring == true || IsReloading == true)
				return false;

			if (_magazineAmmo >= _maxMagazineAmmo)
				return false;

			if (_weaponAmmo <= 0)
				return false;

			if (_cooldown.ExpiredOrNotRunning(Runner) == false)
				return false;

			if (_supressReloadWhileAimed == true && Owner != null && Owner.Character.CharacterController.Data.Aim == true)
				return false;

			return autoReload == false || _magazineAmmo <= 0;
		}

		public override bool CanAim()
		{
			return IsReloading == false;
		}

		public override void Fire(Vector3 firePosition, Vector3 targetPosition, LayerMask hitMask)
		{
			if (CanFire(true) == false)
				return;

			IsFiring = true;

			_magazineAmmo--;

			Vector3 direction = targetPosition - firePosition;

			float distanceToTarget = direction.magnitude;
			direction /= distanceToTarget;

			if (_dispersion > 0f)
			{
				Random.InitState((_projectilesCount + 10) * Runner.Simulation.Tick);
			}

			for (int i = 0; i < _projectilesPerShot; i++)
			{
				var projectileDirection = direction;

				if (_dispersion > 0f)
				{
					var randomDispersion = Random.insideUnitSphere * TotalDispersion;
					projectileDirection = Quaternion.Euler(randomDispersion.x, randomDispersion.y, randomDispersion.z) * direction;
				}

				if (FireProjectile(firePosition, targetPosition, projectileDirection, distanceToTarget, hitMask, i == 0) == true)
				{
					_projectilesCount++;
				}
			}

			if (_dispersion > 0f && _dispersion < _maxFireDispersion)
			{
				_dispersion = Mathf.Clamp(_dispersion + _dispersionIncreasePerShot, _minFireDispersion, _maxFireDispersion);
			}

			_cooldown = TickTimer.CreateFromTicks(Runner, _fireTicks);
		}

		public override void Reload()
		{
			if (CanReload(false) == false)
				return;

			IsReloading = true;

			_cooldown = TickTimer.CreateFromSeconds(Runner, _reloadTime);
			_dispersion = _minFireDispersion;
		}

		public override void AssignFireAudioEffects(Transform root, AudioEffect[] audioEffects)
		{
			root.SetParent(_audioEffectsTransform, false);

			base.AssignFireAudioEffects(root, audioEffects);
		}

		public override bool HasAmmo()
		{
			return _magazineAmmo > 0 || _weaponAmmo > 0;
		}

		public override bool AddAmmo(int ammo)
		{
			if (_weaponAmmo >= _maxWeaponAmmo)
				return false;

			_weaponAmmo = Mathf.Clamp(_weaponAmmo + ammo, 0, _maxWeaponAmmo);
			return true;
		}

		public override bool CanFireToPosition(Vector3 firePosition, ref Vector3 targetPosition, LayerMask hitMask)
		{
			Vector3 direction = targetPosition - firePosition;

			float distanceToTarget = direction.magnitude;
			direction /= distanceToTarget;

			bool positionReached = true;
			int ownerObjectID = Owner != null ? Owner.gameObject.GetInstanceID() : 0;

			if (ProjectileUtility.ProjectileCast(Runner, Object.InputAuthority, ownerObjectID, firePosition, direction, distanceToTarget, distanceToTarget + 1, hitMask, _validHits) == true)
			{
				var hit = _validHits[0];

				if (hit.GameObject != null && hit.GameObject.layer == ObjectLayer.Agent)
				{
					// Do not show "position not reached" indicator if we accidentally hit Agent,
					// this is what we want
					positionReached = true;
					targetPosition = hit.Point;
				}
				else
				{
					positionReached = (hit.Point - targetPosition).sqrMagnitude < 0.5f;
					targetPosition = hit.Point;
				}
			}

			return positionReached;
		}

		protected override void OnWeaponArmed()
		{
			PlayLocalSound(_readySound);
		}

		protected override void OnWeaponDisarmed()
		{
			IsReloading = false;
		}

		// IPickupProvider INTERFACE

		string IDynamicPickupProvider.Description => GetPickupDescription();
		float  IDynamicPickupProvider.DespawnTime => WeaponAmmo == 0 && MagazineAmmo == 0 ? 5f : 60f;

		// FireamWeapon INTERFACE

		protected virtual bool FireProjectile(Vector3 firePosition, Vector3 targetPosition, Vector3 direction, float distanceToTarget, LayerMask hitMask, bool isFirst)
		{
			return false;
		}

		protected virtual void FireVisualProjectile(int projectileIndex, bool playFireEffects)
		{
			if (playFireEffects == true)
			{
				var particlePrefab = Object.HasInputAuthority == true ? _fireParticlePlayer : _fireParticleProxy;
				if (particlePrefab != null)
				{
					var fireParticle = Context.ObjectCache.Get(particlePrefab);
					Context.ObjectCache.ReturnDeferred(fireParticle, HitType == EHitType.Sniper ? 5f : 1f);
					Runner.MoveToRunnerSceneExtended(fireParticle);

					fireParticle.transform.SetParent(_fireTransform, false);
				}

				PlaySound(_fireSound);

				if (Context.ObservedAgent == Owner)
				{
					Context.Camera.ShakeEffect.Play(_cameraShakePosition, EShakeForce.ReplaceSame);
					Context.Camera.ShakeEffect.Play(_cameraShakeRotation, EShakeForce.ReplaceSame);
				}
			}
		}

		// PROTECTED METHODS

		protected void PlayLocalSound(AudioSetup sound)
		{
			if (ApplicationSettings.IsStrippedBatch == true)
				return;
			if (_localAudio.CurrentSetup == sound && _localAudio.IsPlaying == true)
				return;

			_localAudio.Play(sound, EForceBehaviour.ForceAny);
		}

		// PRIVATE METHODS

		private void UpdateVisibleProjectiles()
		{
			if (_lastVisibleProjectileCount == _projectilesCount)
				return;

			if (_proxyWasOutsideOfAOI == true && Object.LastReceiveTick >= Runner.Simulation.Tick - Runner.Config.Simulation.TickRate)
			{
				// Too far behind, do not spawn any fire visuals
				_lastVisibleProjectileCount = _projectilesCount;
				_proxyWasOutsideOfAOI = false;
				return;
			}

			int missingShots = _projectilesCount - _lastVisibleProjectileCount;
			for (int i = 0; i < missingShots; i++)
			{
				FireVisualProjectile(_lastVisibleProjectileCount + i, i == 0);
			}

			_lastVisibleProjectileCount = _projectilesCount;
		}

		private float GetTotalDispersion()
		{
			float multiplier = Owner != null ? Owner.Character.DispersionMultiplier : 1f;
			return Mathf.Clamp(_dispersion * multiplier, _minDispersion, _maxDispersion);
		}

		private string GetPickupDescription()
		{
			// For prefab read initial data
			if (gameObject.scene.rootCount == 0)
			{
				int magazineAmmo = Mathf.Clamp(_initialAmmo, 0, _maxMagazineAmmo);
				int weaponAmmo = Mathf.Clamp(_initialAmmo - magazineAmmo, 0, _maxWeaponAmmo);
				return $"Ammo {magazineAmmo} / {weaponAmmo}";
			}

			return $"Ammo {MagazineAmmo} / {WeaponAmmo}";
		}

		// NETWORK CALLBACKS

		public static void OnStateChanged(Changed<FirearmWeapon> changed)
		{
			if (changed.Behaviour.IsReloading == true)
			{
				changed.LoadOld();

				if (changed.Behaviour.IsReloading == false)
				{
					changed.Behaviour.PlayLocalSound(changed.Behaviour._reloadSound);
				}
			}
		}
	}
}
