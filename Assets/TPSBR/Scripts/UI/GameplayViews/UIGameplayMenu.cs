using UnityEngine;

namespace TPSBR.UI
{
	public class UIGameplayMenu : UICloseView
	{
		// PUBLIC MEMBERS

		public override bool NeedsCursor => _menuVisible;

		// PRIVATE MEMBERS

		[SerializeField]
		private UIButton _leaveButton;
		[SerializeField]
		private UIButton _settingsButton;
		[SerializeField]
		private UIButton _cancelButton;

		private bool _menuVisible;

		// PUBLIC METHODS

		public void Show(bool value, bool force = false)
		{
			if (_menuVisible == value && force == false)
				return;

			_menuVisible = value;
			CanvasGroup.interactable = value;

			(SceneUI as GameplayUI).RefreshCursorVisibility();

			if (value == true)
			{
				Animation.PlayForward();
			}
			else
			{
				Animation.PlayBackward();
			}
		}

		// UIView INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_leaveButton.onClick.AddListener(OnLeaveButton);
			_settingsButton.onClick.AddListener(OnSettingsButton);
			_cancelButton.onClick.AddListener(OnCancelButton);
		}

		protected override void OnDeinitialize()
		{
			_leaveButton.onClick.RemoveListener(OnLeaveButton);
			_settingsButton.onClick.RemoveListener(OnSettingsButton);
			_cancelButton.onClick.RemoveListener(OnCancelButton);

			base.OnDeinitialize();
		}

		protected override void OnOpen()
		{
			base.OnOpen();

			Animation.SampleStart();
			_menuVisible = false;
			CanvasGroup.interactable = false;
		}

		protected override void OnCloseButton()
		{
			Show(false);
		}

		protected override bool OnBackAction()
		{
			if (_menuVisible == true)
				return base.OnBackAction();

			Show(true);
			return true;
		}

		// PRIVATE MEMBERS

		private void OnLeaveButton()
		{
			var dialog = Open<UIYesNoDialogView>();

			dialog.Title.text = "LEAVE MATCH";
			dialog.Description.text = "Are you sure you want to leave current match?";

			dialog.HasClosed += (result) =>
			{
				if (result == true)
				{
					if (Context != null && Context.GameplayMode != null)
					{
						Context.GameplayMode.StopGame();
					}
					else
					{
						Global.Networking.StopGame();
					}
				}
			};
		}

		private void OnSettingsButton()
		{
			var settings = Open<UISettingsView>();
			settings.HasClosed += () => { Show(false); };
		}

		private void OnCancelButton()
		{
			OnCloseButton();
		}
	}
}
