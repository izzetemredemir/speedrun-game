using Fusion;
using Fusion.KCC;

namespace TPSBR
{
	using System;
	using UnityEngine;
	using UnityEngine.InputSystem;
	using TPSBR.UI;

	[OrderBefore(typeof(NetworkCulling), typeof(Agent))]
	public sealed class AgentInput : ContextBehaviour, IBeforeUpdate, IBeforeTick
	{
		// PUBLIC MEMBERS

		/// <summary>
		/// Holds input for fixed update.
		/// </summary>
		public GameplayInput FixedInput { get { CheckFixedAccess(false); return _fixedInput; } }

		/// <summary>
		/// Holds input for current frame render update.
		/// </summary>
		public GameplayInput RenderInput { get { CheckRenderAccess(false); return _renderInput; } }

		/// <summary>
		/// Holds combined inputs from all render frames since last fixed update. Used when Fusion input poll is triggered.
		/// </summary>
		public GameplayInput CachedInput { get { CheckRenderAccess(false); return _cachedInput; } }

		public bool          IsCyclingGrenades => Time.time < _grenadesCyclingStartTime + _grenadesCycleDuration;

		// PRIVATE MEMBERS

		[SerializeField]
		private float         _grenadesCycleDuration = 2f;
		[SerializeField][Range(0.0f, 0.1f)][Tooltip("Look rotation delta for a render frame is calculated as average from all frames within responsivity time.")]
		private float         _lookResponsivity = 0.0f;
		[SerializeField]
		private bool          _logMissingInputs;

		// We need to store last known input to compare current input against (to track actions activation/deactivation). It is also used if an input for current frame is lost.
		// This is not needed on proxies, only input authority is registered to nameof(AgentInput) interest group.
		[Networked(nameof(AgentInput))]
		private GameplayInput _lastKnownInput { get; set; }

		private Agent          _agent;
		private NetworkCulling _networkCulling;
		private GameplayInput  _fixedInput;
		private GameplayInput  _renderInput;
		private GameplayInput  _cachedInput;
		private GameplayInput  _baseFixedInput;
		private GameplayInput  _baseRenderInput;
		private Vector2        _cachedMoveDirection;
		private float          _cachedMoveDirectionSize;
		private bool           _resetCachedInput;
		private int            _missingInputsTotal;
		private int            _missingInputsInRow;
		private int            _logMissingInputFromTick;
		private FrameRecord[]  _frameRecords = new FrameRecord[128];

		private float             _grenadesCyclingStartTime;
		private UIMobileInputView _mobileInputView;

		// PUBLIC METHODS

		/// <summary>
		/// Check if an action is active in current input. FUN/Render input is resolved automatically.
		/// </summary>
		public bool HasActive(EGameplayInputAction action)
		{
			if (Runner.Stage != default)
			{
				CheckFixedAccess(false);
				return action.IsActive(_fixedInput);
			}
			else
			{
				CheckRenderAccess(false);
				return action.IsActive(_renderInput);
			}
		}

		/// <summary>
		/// Check if an action was activated in current input.
		/// In FUN this method compares current fixed input agains previous fixed input.
		/// In Render this method compares current render input against previous render input OR current fixed input (first Render call after FUN).
		/// </summary>
		public bool WasActivated(EGameplayInputAction action)
		{
			if (Runner.Stage != default)
			{
				CheckFixedAccess(false);
				return action.WasActivated(_fixedInput, _baseFixedInput);
			}
			else
			{
				CheckRenderAccess(false);
				return action.WasActivated(_renderInput, _baseRenderInput);
			}
		}

		/// <summary>
		/// Check if an action was activated in custom input.
		/// In FUN this method compares custom input agains previous fixed input.
		/// In Render this method compares custom input against previous render input OR current fixed input (first Render call after FUN).
		/// </summary>
		public bool WasActivated(EGameplayInputAction action, GameplayInput customInput)
		{
			if (Runner.Stage != default)
			{
				CheckFixedAccess(false);
				return action.WasActivated(customInput, _baseFixedInput);
			}
			else
			{
				CheckRenderAccess(false);
				return action.WasActivated(customInput, _baseRenderInput);
			}
		}

		/// <summary>
		/// Check if an action was deactivated in current input.
		/// In FUN this method compares current fixed input agains previous fixed input.
		/// In Render this method compares current render input against previous render input OR current fixed input (first Render call after FUN).
		/// </summary>
		public bool WasDeactivated(EGameplayInputAction action)
		{
			if (Runner.Stage != default)
			{
				CheckFixedAccess(false);
				return action.WasDeactivated(_fixedInput, _baseFixedInput);
			}
			else
			{
				CheckRenderAccess(false);
				return action.WasDeactivated(_renderInput, _baseRenderInput);
			}
		}

		/// <summary>
		/// Check if an action was deactivated in custom input.
		/// In FUN this method compares custom input agains previous fixed input.
		/// In Render this method compares custom input against previous render input OR current fixed input (first Render call after FUN).
		/// </summary>
		public bool WasDeactivated(EGameplayInputAction action, GameplayInput customInput)
		{
			if (Runner.Stage != default)
			{
				CheckFixedAccess(false);
				return action.WasDeactivated(customInput, _baseFixedInput);
			}
			else
			{
				CheckRenderAccess(false);
				return action.WasDeactivated(customInput, _baseRenderInput);
			}
		}

		/// <summary>
		/// Updates fixed input. Use after manipulating with fixed input outside.
		/// </summary>
		/// <param name="fixedInput">Input used in fixed update.</param>
		/// <param name="updateBaseInputs">Updates base fixed input and base render input.</param>
		public void SetFixedInput(GameplayInput fixedInput, bool updateBaseInputs)
		{
			CheckFixedAccess(true);

			_fixedInput = fixedInput;

			if (updateBaseInputs == true)
			{
				_baseFixedInput  = fixedInput;
				_baseRenderInput = fixedInput;
			}
		}

		/// <summary>
		/// Updates render input. Use after manipulating with render input outside.
		/// </summary>
		/// <param name="renderInput">Input used in render update.</param>
		/// <param name="updateBaseInput">Updates base render input.</param>
		public void SetRenderInput(GameplayInput renderInput, bool updateBaseInput)
		{
			CheckRenderAccess(false);

			_renderInput = renderInput;

			if (updateBaseInput == true)
			{
				_baseRenderInput = renderInput;
			}
		}

		/// <summary>
		/// Updates last known input. Use after manipulating with fixed input outside.
		/// </summary>
		/// <param name="fixedInput">Input used as last known input.</param>
		/// <param name="updateBaseInputs">Updates base fixed input and base render input.</param>
		public void SetLastKnownInput(GameplayInput fixedInput, bool updateBaseInputs)
		{
			CheckFixedAccess(true);

			_lastKnownInput = fixedInput;

			if (updateBaseInputs == true)
			{
				_baseFixedInput  = fixedInput;
				_baseRenderInput = fixedInput;
			}
		}

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			// Reset to default state.
			_fixedInput         = default;
			_renderInput        = default;
			_cachedInput        = default;
			_lastKnownInput     = default;
			_baseFixedInput     = default;
			_baseRenderInput    = default;
			_missingInputsTotal = default;
			_missingInputsInRow = default;

			// Wait few seconds before the connection is stable to start tracking missing inputs.
			_logMissingInputFromTick = Runner.Simulation.Tick + Runner.Config.Simulation.TickRate * 4;

			if (Object.HasStateAuthority == true)
			{
				// Only state and input authority works with input and access _lastFixedInput.
				Object.SetInterestGroup(Object.InputAuthority, nameof(AgentInput), true);
			}

			if (_agent.IsLocal == false)
				return;

			// Register local player input polling.
			NetworkEvents networkEvents = Runner.GetComponent<NetworkEvents>();
			networkEvents.OnInput.RemoveListener(OnInput);
			networkEvents.OnInput.AddListener(OnInput);

			// Hide cursor
			Context.Input.RequestCursorVisibility(false, ECursorStateSource.Agent);
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			if (runner != null)
			{
				// Unregister local player input polling.
				NetworkEvents networkEvents = runner.GetComponent<NetworkEvents>();
				networkEvents.OnInput.RemoveListener(OnInput);
			}

			_frameRecords.Clear();

			_mobileInputView = default;
		}

		// IBeforeUpdate INTERFACE

		/// <summary>
		/// 1. Collect input from devices, can be executed multiple times between FixedUpdateNetwork() calls because of faster rendering speed.
		/// </summary>
		void IBeforeUpdate.BeforeUpdate()
		{
			if (Object.HasInputAuthority == false)
				return;

			// Store last render input as a base to current render input.
			_baseRenderInput = _renderInput;

			// Reset input for current frame to default.
			_renderInput = default;

			// Cached input was polled and explicit reset requested.
			if (_resetCachedInput == true)
			{
				_resetCachedInput = false;

				_cachedInput             = default;
				_cachedMoveDirection     = default;
				_cachedMoveDirectionSize = default;
			}

			if (_agent.IsLocal == false || Context.HasInput == false)
				return;
			if ((Context.Input.IsCursorVisible == true && Context.Settings.SimulateMobileInput == false) || Context.GameplayMode.State != GameplayMode.EState.Active)
				return;

			Vector2 moveDirection;
			Vector2 lookRotationDelta;

			if ((Application.isMobilePlatform == false || Application.isEditor == true) && Context.Settings.SimulateMobileInput == false)
			{
				// Standalone input
				// Always use KeyControl.isPressed, Input.GetMouseButton() and Input.GetKey().
				// Never use KeyControl.wasPressedThisFrame, Input.GetMouseButtonDown() or Input.GetKeyDown() otherwise the action might be lost.

				Mouse    mouse      = Mouse.current;
				Keyboard keyboard   = Keyboard.current;
				Vector2  mouseDelta = mouse.delta.ReadValue() * 0.075f;

				moveDirection     = Vector2.zero;
				lookRotationDelta = InputUtility.ProcessLookRotationDelta(_frameRecords, new Vector2(-mouseDelta.y, mouseDelta.x), Global.RuntimeSettings.Sensitivity, _lookResponsivity);

				if (_agent.Character.CharacterController.FixedData.Aim == true)
				{
					lookRotationDelta *= Global.RuntimeSettings.AimSensitivity;
				}

				if (keyboard.wKey.isPressed == true) { moveDirection += Vector2.up;    }
				if (keyboard.sKey.isPressed == true) { moveDirection += Vector2.down;  }
				if (keyboard.aKey.isPressed == true) { moveDirection += Vector2.left;  }
				if (keyboard.dKey.isPressed == true) { moveDirection += Vector2.right; }

				if (moveDirection.IsZero() == false)
				{
					moveDirection.Normalize();
				}

				// Process input for render

				_renderInput.MoveDirection     = moveDirection;
				_renderInput.LookRotationDelta = lookRotationDelta;
				_renderInput.Jump              = keyboard.spaceKey.isPressed;
				_renderInput.Aim               = mouse.rightButton.isPressed;
				_renderInput.Attack            = mouse.leftButton.isPressed;
				_renderInput.Reload            = keyboard.rKey.isPressed;
				_renderInput.Interact          = keyboard.fKey.isPressed;
				_renderInput.Weapon            = GetWeaponInput(keyboard);
				_renderInput.ToggleJetpack     = keyboard.xKey.isPressed;
				_renderInput.Thrust            = keyboard.spaceKey.isPressed;
				_renderInput.ToggleSide        = keyboard.eKey.isPressed;
#if UNITY_EDITOR
				_renderInput.ToggleSpeed       = keyboard.backquoteKey.isPressed;
#else
				_renderInput.ToggleSpeed       = keyboard.leftCtrlKey.isPressed & keyboard.leftAltKey.isPressed & keyboard.backquoteKey.isPressed;
#endif
			}
			else
			{
				// Very basic mobile input, not all actions are implemented

				if (_mobileInputView == null)
				{
					if (Context != null && Context.UI != null)
					{
						_mobileInputView = Context.UI.Get<UIMobileInputView>();
					}

					return;
				}

				const float mobileSensitivityMultiplier = 32.0f;

				moveDirection     = _mobileInputView.Move.normalized;
				lookRotationDelta = InputUtility.ProcessLookRotationDelta(_frameRecords, new Vector2(-_mobileInputView.Look.y, _mobileInputView.Look.x) * mobileSensitivityMultiplier, Global.RuntimeSettings.Sensitivity, _lookResponsivity);

				_mobileInputView.Look = default;

				if (_agent.Character.CharacterController.FixedData.Aim == true)
				{
					lookRotationDelta *= Global.RuntimeSettings.AimSensitivity;
				}

				_renderInput.MoveDirection     = moveDirection;
				_renderInput.LookRotationDelta = lookRotationDelta;
				_renderInput.Jump              = _mobileInputView.Jump;
				_renderInput.Attack            = _mobileInputView.Fire;
				_renderInput.Interact          = _mobileInputView.Interact;
			}

			// Process cached input for next OnInput() call, represents accumulated inputs for all render frames since last fixed update.

			float deltaTime = Time.deltaTime;

			// Move direction accumulation is a special case. Let's say simulation runs 30Hz (33.333ms delta time) and render runs 300Hz (3.333ms delta time).
			// If the player hits W key in last frame before fixed update, the KCC will move in render update by (velocity * 0.003333f).
			// Treating this input the same way for next fixed update results in KCC moving by (velocity * 0.03333f) - 10x more.
			// Following accumulation proportionally scales move direction so it reflects frames in which input was active.
			// This way the next fixed update will correspond more accurately to what happened in render frames.

			_cachedMoveDirection     += moveDirection * deltaTime;
			_cachedMoveDirectionSize += deltaTime;

			_cachedInput.Actions            = new NetworkButtons(_cachedInput.Actions.Bits | _renderInput.Actions.Bits);
			_cachedInput.MoveDirection      = _cachedMoveDirection / _cachedMoveDirectionSize;
			_cachedInput.LookRotationDelta += _renderInput.LookRotationDelta;

			if (_renderInput.Weapon != default)
			{
				_cachedInput.Weapon = _renderInput.Weapon;
			}
		}

		/// <summary>
		/// 3. Read input from Fusion. On input authority the FixedInput will match CachedInput.
		/// </summary>
		void IBeforeTick.BeforeTick()
		{
			if (Object.IsProxy == true || Context == null || Context.GameplayMode == null || Context.GameplayMode.State != GameplayMode.EState.Active)
			{
				_fixedInput      = default;
				_renderInput     = default;
				_cachedInput     = default;
				_lastKnownInput  = default;
				_baseFixedInput  = default;
				_baseRenderInput = default;

				return;
			}

			// Store last known fixed input. This will be compared agaisnt new fixed input.
			_baseFixedInput = _lastKnownInput;

			// Set fixed input to last known fixed input as a fallback.
			_fixedInput = _lastKnownInput;

			if (Object.InputAuthority != PlayerRef.None)
			{
				// If this fails, fallback (last known) input will be used as current.
				if (Runner.TryGetInputForPlayer(Object.InputAuthority, out GameplayInput input) == true)
				{
					// New input received, we can store it.
					_fixedInput = input;

					// Update last known input. Will be used next tick as base and fallback.
					_lastKnownInput = input;

					if (Runner.Simulation.Stage == SimulationStages.Forward)
					{
						_missingInputsInRow = 0;
					}
				}
				else
				{
					if (Runner.Simulation.Stage == SimulationStages.Forward)
					{
						++_missingInputsInRow;
						++_missingInputsTotal;

						if (_missingInputsInRow > 5)
						{
							_fixedInput.LookRotationDelta = default;
						}
						else if (_missingInputsInRow > 2)
						{
							_fixedInput.LookRotationDelta *= 0.5f;
						}

						if (_logMissingInputs == true && Runner.Simulation.Tick >= _logMissingInputFromTick)
						{
							Debug.LogWarning($"Missing input for {Object.InputAuthority} {Runner.Simulation.Tick}. In Row: {_missingInputsInRow} Total: {_missingInputsTotal}", gameObject);
						}
					}
				}
			}

			// The current fixed input will be used as a base to first Render after FUN.
			_baseRenderInput = _fixedInput;
		}

		// PRIVATE METHODS

		private void Awake()
		{
			_agent          = GetComponent<Agent>();
			_networkCulling = GetComponent<NetworkCulling>();
		}

		/// <summary>
		/// 2. Push cached input and reset properties, can be executed multiple times within single Unity frame if the rendering speed is slower than Fusion simulation (or there is a performance spike).
		/// </summary>
		private void OnInput(NetworkRunner runner, NetworkInput networkInput)
		{
			if (_agent.IsLocal == false || Context.HasInput == false)
			{
				_cachedInput = default;
				_renderInput = default;
				return;
			}

			GameplayInput gameplayInput = _cachedInput;

			// Input is polled for single fixed update, but at this time we don't know how many times in a row OnInput() will be executed.
			// This is the reason for having a reset flag instead of resetting input immediately, otherwise we could lose input for next fixed updates (for example move direction).

			_resetCachedInput = true;

			// Now we reset all properties which should not propagate into next OnInput() call (for example LookRotationDelta - this must be applied only once and reset immediately).
			// If there's a spike, OnInput() and FixedUpdateNetwork() will be called multiple times in a row without BeforeUpdate() in between, so we don't reset move direction to preserve movement.
			// Instead, move direction and other sensitive properties are reset in next BeforeUpdate() - driven by _resetCachedInput.

			_cachedInput.LookRotationDelta = default;

			// Input consumed by OnInput() call will be read in FixedUpdateNetwork() and immediately propagated to KCC.
			// Here we should reset render properties so they are not applied twice (fixed + render update).

			_renderInput.LookRotationDelta = default;

			networkInput.Set(gameplayInput);
		}

		private byte GetWeaponInput(Keyboard keyboard)
		{
			if (keyboard.qKey.wasPressedThisFrame == true)
				return (byte)(_agent.Weapons.PreviousWeaponSlot + 1); // Fast switch

			int weaponSlot = -1;

			if (keyboard.digit1Key.wasPressedThisFrame == true) { weaponSlot = 0; }
			if (keyboard.digit2Key.wasPressedThisFrame == true) { weaponSlot = 1; }
			if (keyboard.digit3Key.wasPressedThisFrame == true) { weaponSlot = 2; }
			if (keyboard.digit4Key.wasPressedThisFrame == true) { weaponSlot = 3; }
			if (keyboard.digit5Key.wasPressedThisFrame == true) { weaponSlot = 4; }

			if (weaponSlot < 0 && keyboard.gKey.wasPressedThisFrame == true)
			{
				weaponSlot = 3; // Cycle grenades
			}

			if (weaponSlot < 0)
				return 0;

			if (weaponSlot <= 2)
				return (byte)(weaponSlot + 1); // Standard weapon switch

			// Grenades (grenades are under slot 5, 6, 7 - but we cycle them with 4 numped key)
			if (weaponSlot == 3)
			{
				int pendingWeapon = _agent.Weapons.PendingWeaponSlot;
				int grenadesStart = IsCyclingGrenades == true && pendingWeapon < 7 ? Mathf.Max(pendingWeapon, 4) : 4;

				int grenadeToSwitch = _agent.Weapons.GetNextWeaponSlot(grenadesStart, 4);

				_grenadesCyclingStartTime = Time.time;

				if (grenadeToSwitch > 0 && grenadeToSwitch != pendingWeapon)
				{
					return (byte)(grenadeToSwitch + 1);
				}
			}

			return 0;
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
		private void CheckFixedAccess(bool checkStage)
		{
			if (checkStage == true && Runner.Stage == default)
			{
				throw new InvalidOperationException("This call should be executed from FixedUpdateNetwork!");
			}

			if (Runner.Stage != default && Object.IsProxy == true)
			{
				throw new InvalidOperationException("Fixed input is available only on State & Input authority!");
			}
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
		private void CheckRenderAccess(bool checkStage)
		{
			if (checkStage == true && Runner.Stage != default)
			{
				throw new InvalidOperationException("This call should be executed outside of FixedUpdateNetwork!");
			}

			if (Runner.Stage == default && Object.HasInputAuthority == false)
			{
				throw new InvalidOperationException("Render and cached inputs are available only on Input authority!");
			}
		}
	}
}
