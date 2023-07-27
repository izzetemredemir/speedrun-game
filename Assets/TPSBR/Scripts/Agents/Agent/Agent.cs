using Fusion;
using Fusion.KCC;

namespace TPSBR
{
	using UnityEngine;
	using UnityEngine.Profiling;

	[OrderBefore(typeof(HitboxManager))]
	public sealed class Agent : ContextBehaviour
	{
		// PUBLIC METHODS

		public bool        IsLocal    => Object != null && Object.HasInputAuthority == true;
		public bool        IsObserved => Context != null && Context.ObservedAgent == this;

		public AgentInput  AgentInput => _agentInput;
		public Character   Character  => _character;
		public Weapons     Weapons    => _weapons;
		public Health      Health     => _health;
		public AgentSenses Senses     => _senses;
		public Jetpack     Jetpack    => _jetpack;
		public AgentVFX    Effects    => _agentVFX;

		[Networked]
		public NetworkBool LeftSide { get; private set; }

		// PRIVATE MEMBERS

		[SerializeField]
		private float _jumpPower;
		[SerializeField]
		private float _topCameraAngleLimit;
		[SerializeField]
		private float _bottomCameraAngleLimit;
		[SerializeField]
		private KCCProcessor _fastMovementProcessor;
		[SerializeField]
		private GameObject _visualRoot;

		[Header("Fall Damage")]
		[SerializeField]
		private float _minFallDamage = 5f;
		[SerializeField]
		private float _maxFallDamage = 200f;
		[SerializeField]
		private float _maxFallDamageVelocity = 20f;
		[SerializeField]
		private float _minFallDamageVelocity = 5f;

		private AgentInput     _agentInput;
		private Character      _character;
		private Weapons        _weapons;
		private Jetpack        _jetpack;
		private AgentSenses    _senses;
		private Health         _health;
		private AgentVFX       _agentVFX;
		private NetworkCulling _networkCulling;
		private Quaternion     _cachedLookRotation;
		private Quaternion     _cachedPitchRotation;

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			name = Object.InputAuthority.ToString();

			var earlyAgentController = GetComponent<EarlyAgentController>();
			earlyAgentController.SetDelegates(OnEarlyFixedUpdate, OnEarlyRender);

			var lateAgentController = GetComponent<LateAgentController>();
			lateAgentController.SetDelegates(OnLateFixedUpdate, OnLateRender);

			_visualRoot.SetActive(true);

			_character.OnSpawned(this);
			_jetpack.OnSpawned(this);
			_weapons.OnSpawned();
			_health.OnSpawned(this);
			_agentVFX.OnSpawned(this);

			if (ApplicationSettings.IsStrippedBatch == true)
			{
				gameObject.SetActive(false);

				if (ApplicationSettings.GenerateInput == true)
				{
					NetworkEvents networkEvents = Runner.GetComponent<NetworkEvents>();
					networkEvents.OnInput.RemoveListener(GenerateRandomInput);
					networkEvents.OnInput.AddListener(GenerateRandomInput);
				}
			}

			void GenerateRandomInput(NetworkRunner runner, NetworkInput networkInput)
			{
				// Used for batch testing

				GameplayInput gameplayInput = new GameplayInput();
				gameplayInput.MoveDirection     = new Vector2(UnityEngine.Random.value * 2.0f - 1.0f, UnityEngine.Random.value > 0.25f ? 1.0f : -1.0f).normalized;
				gameplayInput.LookRotationDelta = new Vector2(UnityEngine.Random.value * 2.0f - 1.0f, UnityEngine.Random.value * 2.0f - 1.0f);
				gameplayInput.Jump              = UnityEngine.Random.value > 0.99f;
				gameplayInput.Attack            = UnityEngine.Random.value > 0.99f;
				gameplayInput.Reload            = UnityEngine.Random.value > 0.99f;
				gameplayInput.Interact          = UnityEngine.Random.value > 0.99f;
				gameplayInput.Weapon            = (byte)(UnityEngine.Random.value > 0.99f ? (UnityEngine.Random.value > 0.25f ? 2 : 1) : 0);

				networkInput.Set(gameplayInput);
			}
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			if (_weapons != null)
			{
				_weapons.OnDespawned();
			}

			if (_jetpack != null)
			{
				_jetpack.OnDespawned();
			}

			if (_health != null)
			{
				_health.OnDespawned();
			}

			if (_agentVFX != null)
			{
				_agentVFX.OnDespawned();
			}

			var earlyAgentController = GetComponent<EarlyAgentController>();
			earlyAgentController.SetDelegates(null, null);

			var lateAgentController = GetComponent<LateAgentController>();
			lateAgentController.SetDelegates(null, null);
		}

		public override void FixedUpdateNetwork()
		{
			if (_networkCulling.IsCulled == true)
				return;

			// Interpolate proxies before HitboxManager so positions are correct for lag compensation
			if (Runner.IsForward == true && Object.IsProxy == true)
			{
				_character.Interpolate();
			}

			// Performance optimization, unnecessary euler call
			Quaternion currentLookRotation = _character.CharacterController.FixedData.LookRotation;
			if (_cachedLookRotation.ComponentEquals(currentLookRotation) == false)
			{
				_cachedLookRotation  = currentLookRotation;
				_cachedPitchRotation = Quaternion.Euler(_character.CharacterController.FixedData.LookPitch, 0.0f, 0.0f);
			}

			_character.GetCameraHandle().transform.localRotation = _cachedPitchRotation;

			CheckFallDamage();
		}

		public override void Render()
		{
			if (_networkCulling.IsCulled == true)
				return;

			if (Object.HasInputAuthority == true)
			{
				// Performance optimization, unnecessary euler call
				Quaternion currentLookRotation = _character.CharacterController.RenderData.LookRotation;
				if (_cachedLookRotation.ComponentEquals(currentLookRotation) == false)
				{
					_cachedLookRotation  = currentLookRotation;
					_cachedPitchRotation = Quaternion.Euler(_character.CharacterController.RenderData.LookPitch, 0.0f, 0.0f);
				}

				_character.GetCameraHandle().transform.localRotation = _cachedPitchRotation;
			}
		}

		// MONOBEHAVIOUR

		private void Awake()
		{
			_agentInput     = GetComponent<AgentInput>();
			_character      = GetComponent<Character>();
			_weapons        = GetComponent<Weapons>();
			_health         = GetComponent<Health>();
			_agentVFX       = GetComponent<AgentVFX>();
			_senses         = GetComponent<AgentSenses>();
			_jetpack        = GetComponent<Jetpack>();
			_networkCulling = GetComponent<NetworkCulling>();

			if (ApplicationSettings.IsStrippedBatch == true)
				return;

			_networkCulling.Updated += OnCullingUpdated;
		}

		// PRIVATE METHODS

		private void OnEarlyFixedUpdate()
		{
			if (_networkCulling.IsCulled == true)
				return;

			Profiler.BeginSample(nameof(Agent));

			ProcessFixedInput();

			_weapons.OnFixedUpdate();
			_jetpack.OnFixedUpdate();
			_character.OnFixedUpdate();

			Profiler.EndSample();
		}

		private void OnLateFixedUpdate()
		{
			if (_networkCulling.IsCulled == true)
				return;

			if (Object.IsProxy == false && _health.IsAlive == true)
			{
				bool attackWasActivated   = _agentInput.WasActivated(EGameplayInputAction.Attack);
				bool reloadWasActivated   = _agentInput.WasActivated(EGameplayInputAction.Reload);
				bool interactWasActivated = _agentInput.WasActivated(EGameplayInputAction.Interact);

				TryFire(attackWasActivated, _agentInput.FixedInput.Attack);
				TryReload(reloadWasActivated == false);
				_weapons.TryInteract(interactWasActivated, _agentInput.FixedInput.Interact);
			}

			_weapons.OnLateFixedUpdate();
			_health.OnFixedUpdate();

			if (Object.IsProxy == false)
			{
				_agentInput.SetLastKnownInput(_agentInput.FixedInput, true);
			}
		}

		private void OnEarlyRender()
		{
			if (_networkCulling.IsCulled == true)
				return;

			ProcessRenderInput();

			_character.OnRender();
			_weapons.OnRender();
		}

		private void OnLateRender()
		{
			if (_networkCulling.IsCulled == true)
				return;

			_character.OnLateRender();
		}

		private void ProcessFixedInput()
		{
			if (Object.IsProxy == true)
				return;

			KCC     kcc          = _character.CharacterController;
			KCCData kccFixedData = kcc.FixedData;

			GameplayInput input = default;

			if (_health.IsAlive == true)
			{
				input = _agentInput.FixedInput;
			}

			if (input.Aim == true)
			{
				input.Aim &= CanAim(kccFixedData);
			}

			if (input.Aim == true)
			{
				if (_weapons.CurrentWeapon != null && _weapons.CurrentWeapon.HitType == EHitType.Sniper)
				{
					input.LookRotationDelta *= 0.3f;
				}
			}

			kcc.SetAim(input.Aim);

			if (_agentInput.WasActivated(EGameplayInputAction.Jump, input) == true && _character.AnimationController.CanJump() == true)
			{
				kcc.Jump(Vector3.up * _jumpPower);
			}

			SetLookRotation(kccFixedData, input.LookRotationDelta, _weapons.GetRecoil(), out Vector2 newRecoil);
			_weapons.SetRecoil(newRecoil);

			kcc.SetInputDirection(input.MoveDirection.IsZero() == true ? Vector3.zero : kcc.FixedData.TransformRotation * input.MoveDirection.X0Y());

			if (_agentInput.WasActivated(EGameplayInputAction.ToggleSide, input) == true)
			{
				LeftSide = !LeftSide;
			}

			if (_agentInput.WasActivated(EGameplayInputAction.ToggleSpeed, input) == true)
			{
				if (kcc.HasModifier(_fastMovementProcessor) == true)
				{
					kcc.RemoveModifier(_fastMovementProcessor);
				}
				else
				{
					kcc.AddModifier(_fastMovementProcessor);
				}
			}

			if (input.Weapon > 0 && _character.AnimationController.CanSwitchWeapons(true) == true && _weapons.SwitchWeapon(input.Weapon - 1) == true)
			{
				_character.AnimationController.SwitchWeapons();
			}
			else if (input.Weapon <= 0 && _weapons.PendingWeaponSlot != _weapons.CurrentWeaponSlot && _character.AnimationController.CanSwitchWeapons(false) == true)
			{
				_character.AnimationController.SwitchWeapons();
			}

			if (_agentInput.WasActivated(EGameplayInputAction.ToggleJetpack, input) == true)
			{
				if (_jetpack.IsActive == true)
				{
					_jetpack.Deactivate();
				}
				else if (_character.AnimationController.CanSwitchWeapons(true) == true)
				{
					_jetpack.Activate();
				}
			}

			if (_jetpack.IsActive == true)
			{
				_jetpack.FullThrust = input.Thrust;
			}

			_agentInput.SetFixedInput(input, false);
		}

		private void ProcessRenderInput()
		{
			if (Object.HasInputAuthority == false)
				return;

			KCC     kcc           = _character.CharacterController;
			KCCData kccFixedData  = kcc.FixedData;

			GameplayInput input = default;

			if (_health.IsAlive == true)
			{
				input = _agentInput.RenderInput;

				var cachedInput = _agentInput.CachedInput;

				input.LookRotationDelta = cachedInput.LookRotationDelta;
				input.Aim               = cachedInput.Aim;
				input.Thrust            = cachedInput.Thrust;
			}

			if (input.Aim == true)
			{
				input.Aim &= CanAim(kccFixedData);
			}

			if (input.Aim == true)
			{
				if (_weapons.CurrentWeapon != null && _weapons.CurrentWeapon.HitType == EHitType.Sniper)
				{
					input.LookRotationDelta *= 0.3f;
				}
			}

			SetLookRotation(kccFixedData, input.LookRotationDelta, _weapons.GetRecoil(), out Vector2 newRecoil);

			kcc.SetInputDirection(input.MoveDirection.IsZero() == true ? Vector3.zero : kcc.RenderData.TransformRotation * input.MoveDirection.X0Y());

			kcc.SetAim(input.Aim);

			if (_agentInput.WasActivated(EGameplayInputAction.Jump, input) == true && _character.AnimationController.CanJump() == true)
			{
				kcc.Jump(Vector3.up * _jumpPower);
			}
		}

		private void TryFire(bool attack, bool hold)
		{
			var currentWeapon = _weapons.CurrentWeapon;
			if (currentWeapon is ThrowableWeapon && currentWeapon.WeaponSlot == _weapons.PendingWeaponSlot)
			{
				// Fire is handled form the grenade animation state itself
				_character.AnimationController.ProcessThrow(attack, hold);
				return;
			}

			if (hold == false)
				return;
			if (_weapons.CanFireWeapon(attack) == false)
				return;

			if (_character.AnimationController.StartFire() == true)
			{
				if (_weapons.Fire() == true)
				{
					_health.ResetRegenDelay();
				}
			}
		}

		private void TryReload(bool autoReload)
		{
			if (_weapons.CanReloadWeapon(autoReload) == false)
				return;

			if (_character.AnimationController.StartReload() == true)
			{
				_weapons.Reload();
			}
		}

		private bool CanAim(KCCData kccData)
		{
			if (kccData.IsGrounded == false)
				return false;

			return _weapons.CanAim();
		}

		private void SetLookRotation(KCCData kccData, Vector2 lookRotationDelta, Vector2 recoil, out Vector2 newRecoil)
		{
			if (lookRotationDelta.IsZero() == true && recoil.IsZero() == true && _character.CharacterController.Data.Recoil == Vector2.zero)
			{
				newRecoil = recoil;
				return;
			}

			Vector2 baseLookRotation = kccData.GetLookRotation(true, true) - kccData.Recoil;
			Vector2 recoilReduction  = Vector2.zero;

			if (recoil.x > 0f && lookRotationDelta.x < 0)
			{
				recoilReduction.x = Mathf.Clamp(lookRotationDelta.x, -recoil.x, 0f);
			}

			if (recoil.x < 0f && lookRotationDelta.x > 0f)
			{
				recoilReduction.x = Mathf.Clamp(lookRotationDelta.x, 0, -recoil.x);
			}

			if (recoil.y > 0f && lookRotationDelta.y < 0)
			{
				recoilReduction.y = Mathf.Clamp(lookRotationDelta.y, -recoil.y, 0f);
			}

			if (recoil.y < 0f && lookRotationDelta.y > 0f)
			{
				recoilReduction.y = Mathf.Clamp(lookRotationDelta.y, 0, -recoil.y);
			}

			lookRotationDelta -= recoilReduction;
			recoil            += recoilReduction;

			lookRotationDelta.x = Mathf.Clamp(baseLookRotation.x + lookRotationDelta.x, -_topCameraAngleLimit, _bottomCameraAngleLimit) - baseLookRotation.x;

			_character.CharacterController.SetLookRotation(baseLookRotation + recoil + lookRotationDelta);
			_character.CharacterController.SetRecoil(recoil);

			_character.AnimationController.Turn(lookRotationDelta.y);

			newRecoil = recoil;
		}

		private void CheckFallDamage()
		{
			if (Object.IsProxy == true)
				return;

			if (_health.IsAlive == false)
				return;

			var kccData = _character.CharacterController.Data;

			if (kccData.IsGrounded == false || kccData.WasGrounded == true)
				return;

			float fallVelocity = -kccData.DesiredVelocity.y;
			for (int i = 1; i < 3; ++i)
			{
				var historyData = _character.CharacterController.GetHistory(kccData.Tick - i);
				if (historyData != null)
				{
					fallVelocity = Mathf.Max(fallVelocity, -historyData.DesiredVelocity.y);
				}
			}

			if (fallVelocity < 0f)
				return;

			float damage = MathUtility.Map(_minFallDamageVelocity, _maxFallDamageVelocity, 0f, _maxFallDamage, fallVelocity);

			if (damage <= _minFallDamage)
				return;

			var hitData = new HitData
			{
				Action           = EHitAction.Damage,
				Amount           = damage,
				Position         = transform.position,
				Normal           = Vector3.up,
				Direction        = -Vector3.up,
				InstigatorRef    = Object.InputAuthority,
				Instigator       = _health,
				Target           = _health,
				HitType          = EHitType.Suicide,
			};

			(_health as IHitTarget).ProcessHit(ref hitData);
		}

		private void OnCullingUpdated(bool isCulled)
		{
			bool isActive = isCulled == false;

			// Show/hide the game object based on AoI (Area of Interest)

			_visualRoot.SetActive(isActive);

			if (_character.CharacterController.Collider != null)
			{
				_character.CharacterController.Collider.enabled = isActive;
			}
		}
	}
}
