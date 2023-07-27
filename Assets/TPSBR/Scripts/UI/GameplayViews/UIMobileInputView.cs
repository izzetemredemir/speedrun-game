namespace TPSBR.UI
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.EventSystems;
	using UnityEngine.InputSystem;
	using UnityEngine.InputSystem.Controls;
	using UnityEngine.InputSystem.Utilities;

	using TouchPhase = UnityEngine.InputSystem.TouchPhase;

	public sealed class UIMobileInputView : UIView, IPointerDownHandler
	{
		// PUBLIC MEMBERS

		public Vector2 Move     { get; set; }
		public Vector2 Look     { get; set; }
		public bool    Fire     { get; set; }
		public bool    Jump     { get; set; }
		public bool    Interact { get; set; }

		// PRIVATE MEMBERS

		[SerializeField]
		private bool          _resetMoveJoystickAfterMove;
		[SerializeField]
		private float         _joystickRadius;
		[SerializeField]
		private bool          _moveJoystickOrigin;

		[Header("References")]
		[SerializeField]
		private UIBehaviour   _root;
		[SerializeField]
		private RectTransform _move;
		[SerializeField]
		private RectTransform _look;
		[SerializeField]
		private RectTransform _jump;
		[SerializeField]
		private RectTransform _fire;
		[SerializeField]
		private RectTransform _interact;
		[SerializeField]
		private RectTransform _joystick;
		[SerializeField]
		private UIBehaviour   _joystickOrigin;

		private int     _movePointerID;
		private Vector2 _movePosition;
		private Vector2 _moveOrigin;

		private int     _lookPointerID;
		private Vector2 _lookPosition;
		private Vector2 _lookDelta;
		private bool    _isFiring;
		private bool    _isJumping;
		private bool    _isInteracting;

		private Vector2 _joystickInitialPosition;

		private Rect    _moveRect;
		private Rect    _lookRect;

		private List<int>           _activeTouches   = new List<int>();
		private List<int>           _inactiveTouches = new List<int>();
		private List<RectTransform> _ignoredAreas    = new List<RectTransform>();
		private List<Rect>          _ignoredRects    = new List<Rect>();

		// PUBLIC METHODS

		public void RegisterIgnoredArea(RectTransform transform)
		{
			if (_ignoredAreas.Contains(transform) == true)
				return;

			_ignoredAreas.Add(transform);
			_ignoredRects.Add(GetScreenSpaceRect(transform));
		}

		public void UnregisterIgnoredArea(RectTransform transform)
		{
			int index = _ignoredAreas.IndexOf(transform);
			if (index < 0)
				return;

			_ignoredAreas.RemoveBySwap(index);
			_ignoredRects.RemoveBySwap(index);
		}

		// UIView INTERFACE

		protected override void OnInitialize()
		{
			_joystickInitialPosition = _joystick.position;
			_joystickOrigin.CanvasGroup.alpha = 0.0f;

			ToggleState(false);
		}

		protected override void OnVisible()
		{
			_activeTouches.Clear();
			_inactiveTouches.Clear();

			_lookRect = GetScreenSpaceRect(_look);
			_moveRect = GetScreenSpaceRect(_move);

			_isFiring      = false;
			_isJumping     = false;
			_isInteracting = false;
		}

		protected override void OnTick()
		{
			if ((Application.isMobilePlatform == false || Application.isEditor == true) && Context.Settings.SimulateMobileInput == false)
			{
				ToggleState(false);
				return;
			}

			ProcessTouches();
			ProcessMouse();

			if (_root.CanvasGroup.IsActive() == false && Context.LocalPlayerRef == Context.ObservedPlayerRef && Context.LocalPlayerRef.IsValid == true)
			{
				ToggleState(true);
			}
			else if (_root.CanvasGroup.IsActive() == true && (Context.LocalPlayerRef != Context.ObservedPlayerRef || Context.LocalPlayerRef.IsValid == false))
			{
				ToggleState(false);
			}

			Move = _movePointerID >= 0 ? InputUtility.PixelsToCentimeters(_movePosition - _moveOrigin) : default;

			if (_lookPointerID >= 0)
			{
				Look += InputUtility.PixelsToCentimeters(_lookDelta);

				if (_isFiring      == true) { Fire     = true; }
				if (_isJumping     == true) { Jump     = true; }
				if (_isInteracting == true) { Interact = true; }

				_lookDelta = default;
			}
			else
			{
				Fire     = false; _isFiring      = false;
				Jump     = false; _isJumping     = false;
				Interact = false; _isInteracting = false;
			}
		}

		// IPointerDownHandler INTERFACE

		void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
		{
			if (eventData.pointerEnter == _fire.gameObject)     { Fire     = true; _isFiring      = true; }
			if (eventData.pointerEnter == _jump.gameObject)     { Jump     = true; _isJumping     = true; }
			if (eventData.pointerEnter == _interact.gameObject) { Interact = true; _isInteracting = true; }
		}

		// PRIVATE METHODS

		private void ProcessTouches()
		{
			Touchscreen touchscreen = Touchscreen.current;
			if (touchscreen == null)
				return;

			_inactiveTouches.Clear();
			_inactiveTouches.AddRange(_activeTouches);

			ReadOnlyArray<TouchControl> touches = touchscreen.touches;
			for (int i = 0, count = touches.Count; i < count; i++)
			{
				TouchControl touch   = touches[i];
				TouchPhase   phase   = touch.phase.ReadValue();
				int          touchID = touch.touchId.ReadValue();

				_inactiveTouches.Remove(touchID);

				if (phase == TouchPhase.None)
				{
					// Nothing
				}
				else if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
				{
					ProcessTouchEnded(touchID);
				}
				else
				{
					if (_activeTouches.Contains(touchID) == false)
					{
						_activeTouches.Add(touchID);

						ProcessTouchBegan(touchID, touch.position.ReadValue());
					}
					else
					{
						ProcessTouchMoved(touchID, touch.position.ReadValue());
					}
				}
			}

			for (int i = 0; i < _inactiveTouches.Count; ++i)
			{
				ProcessTouchEnded(_inactiveTouches[i]);
			}
			_inactiveTouches.Clear();
		}

		private void ProcessMouse()
		{
#if UNITY_EDITOR
			Mouse mouse = Mouse.current;
			if (mouse == null)
				return;

			if (mouse.leftButton.wasPressedThisFrame == true)
			{
				ProcessTouchBegan(int.MaxValue, mouse.position.ReadValue());
			}
			else if (mouse.leftButton.isPressed == true)
			{
				ProcessTouchMoved(int.MaxValue, mouse.position.ReadValue());
			}
			else if (mouse.leftButton.wasReleasedThisFrame == true)
			{
				ProcessTouchEnded(int.MaxValue);
			}
#endif
		}

		private void ProcessTouchBegan(int touchID, Vector2 touchPosition)
		{
			for (int i = 0, count = _ignoredRects.Count; i < count; i++)
			{
				Rect ignoredRect = _ignoredRects[i];
				if (ignoredRect.Contains(touchPosition) == true)
					return;
			}

			if (_moveRect.Contains(touchPosition) == true)
			{
				_movePointerID = touchID;
				_movePosition  = touchPosition;
				_moveOrigin    = _movePosition;

				_joystick.position = _movePosition;

				_joystickOrigin.RectTransform.position = _movePosition;
				_joystickOrigin.CanvasGroup.alpha = 1.0f;
			}
			else if (_lookRect.Contains(touchPosition) == true)
			{
				_lookPointerID = touchID;
				_lookPosition  = touchPosition;
				_lookDelta     = default;
			}
		}

		private void ProcessTouchMoved(int touchID, Vector2 touchPosition)
		{
			if (touchID == _movePointerID)
			{
				_movePosition = touchPosition;

				Vector2 direction    = _movePosition - _moveOrigin;
				float   scaledRadius = _joystickRadius * transform.lossyScale.x;

				if (scaledRadius > 0.0f && direction.sqrMagnitude > scaledRadius * scaledRadius)
				{
					if (_moveJoystickOrigin == true)
					{
						_joystick.position                 = _movePosition;
						_moveOrigin                        = _movePosition - scaledRadius * direction.normalized;
						_joystickOrigin.transform.position = _moveOrigin;
					}
					else
					{
						_joystick.position = _moveOrigin + direction.normalized * scaledRadius;;
					}
				}
				else
				{
					_joystick.position = _movePosition;
				}
			}
			else if (touchID == _lookPointerID)
			{
				Vector2 position = touchPosition;

				_lookDelta    = position - _lookPosition;
				_lookPosition = position;
			}
		}

		private void ProcessTouchEnded(int touchID)
		{
			if (touchID == _movePointerID)
			{
				_movePointerID = -1;

				if (_resetMoveJoystickAfterMove == true)
				{
					_joystick.position = _joystickInitialPosition;
				}
				else
				{
					_joystick.position = _moveOrigin;
				}

				_joystickOrigin.CanvasGroup.alpha = 0.0f;

				Move = default;
			}
			else if (touchID == _lookPointerID)
			{
				_lookPointerID = -1;
				Look = default;
			}
		}

		private void ToggleState(bool isActive)
		{
			_joystick.position  = _joystickInitialPosition;

			_movePointerID  = -1;
			_movePosition   = default;
			_moveOrigin     = default;

			_lookPointerID  = -1;
			_lookPosition   = default;
			_lookDelta      = default;

			_movePointerID  = -1;
			_lookPointerID  = -1;

			_root.CanvasGroup.SetActive(isActive);
		}

		private Rect GetScreenSpaceRect(RectTransform transform)
		{
			Canvas canvas = transform.GetComponent<Canvas>();
			if (canvas == null)
			{
				canvas = transform.GetComponentInParent<Canvas>();
			}

			Rect rect  = transform.rect;
			rect.size *= canvas.scaleFactor;

			if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
			{
				rect.center = canvas.worldCamera.WorldToScreenPoint(transform.position);
			}
			else if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
			{
				rect.center =  transform.position;
			}

			return rect;
		}
	}
}
