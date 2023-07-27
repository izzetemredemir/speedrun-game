using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TPSBR
{
	[Flags]
	public enum ECursorStateSource
	{
		None,
		UI,
		Menu,
		Agent,
	}

	public class SceneInput : SceneService
	{
		// PUBLIC MEMBERS

		public bool IsCursorVisible => _cursorVisibilitySources != ECursorStateSource.None;

		// PRIVATE MEMBERS

		private List<IBackHandler> _backHandlers = new List<IBackHandler>();
		private ECursorStateSource _cursorVisibilitySources;

		private bool _hasInput;

		// PUBLIC METHODS

		public void RequestCursorVisibility(bool isVisible, ECursorStateSource source, bool force = true)
		{
			if (source == ECursorStateSource.None)
				return;

			var previousSources = _cursorVisibilitySources;

			if (isVisible == true)
			{
				_cursorVisibilitySources = _cursorVisibilitySources | source;
			}
			else
			{
				_cursorVisibilitySources = _cursorVisibilitySources & ~source;
			}

			if (_cursorVisibilitySources != previousSources || force == true)
			{
				RefreshCursor();
			}
		}

		public void ClearCursorLock()
		{
			_cursorVisibilitySources = ECursorStateSource.None;
			RefreshCursor();
		}

		public void TrigggerBackAction()
		{
			BackAction();
		}

		// SceneService INTERFACE

		protected override void OnTick()
		{
			base.OnTick();

			if (Context.Runner != null)
			{
				if (ApplicationSettings.IsStrippedBatch == true && ApplicationSettings.GenerateInput == true)
				{
					Context.Runner.ProvideInput = true;
				}
				else
				{
					Context.Runner.ProvideInput = Context.HasInput;
				}
			}

			if (Context.HasInput == true || Scene is Menu)
			{
				if (Keyboard.current.escapeKey.wasPressedThisFrame == true)
				{
					BackAction();
				}
			}

			if (Context.HasInput == true)
			{
				bool toggleCursor = Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame;
#if !UNITY_EDITOR
				toggleCursor &= Keyboard.current.leftCtrlKey.isPressed;
#endif
				if (toggleCursor == true)
				{
					RequestCursorVisibility(IsCursorVisible == false, ECursorStateSource.Agent);
				}
			}

			if (_hasInput != Context.HasInput)
			{
				// Refresh cursor when input changed (e.g. when switching between peers)
				RefreshCursor();

				_hasInput = Context.HasInput;
			}
		}

		// PRIVATE METHODS

		private void BackAction()
		{
			if (_backHandlers.Count == 0)
			{
				Context.UI.GetAll(_backHandlers);
				_backHandlers.Add(Context.UI);

				_backHandlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
			}

			for (int i = 0, count = _backHandlers.Count; i < count; ++i)
			{
				IBackHandler handler = _backHandlers[i];
				if (handler.IsActive == true && handler.OnBackAction() == true)
					break;
			}
		}

		private void RefreshCursor()
		{
			if (IsActive == false)
				return;

			if (Context != null && Context.HasInput == false)
				return;

			if (IsCursorVisible == true)
			{
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible   = true;
			}
			else
			{
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
			}
		}
	}
}
