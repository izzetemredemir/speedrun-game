using UnityEngine;

namespace TPSBR.UI
{
	public class UICloseView : UIView 
	{
		// PUBLIC MEMBERS

		public UIView BackView { get; set; }

		public UIButton CloseButton => _closeButton;

		// PRIVATE MEMBERS
		
		[SerializeField]
		private UIButton _closeButton;

		// PUBLIC METHODS

		public void CloseWithBack()
		{
			OnCloseButton();
		}

		// UIVIEW INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			if (_closeButton != null)
			{
				_closeButton.onClick.AddListener(OnCloseButton);
			}
		}

		protected override void OnDeinitialize()
		{
			if (_closeButton != null)
			{
				_closeButton.onClick.RemoveListener(OnCloseButton);
			}

			base.OnDeinitialize();
		}

		protected override bool OnBackAction()
		{
			if (IsInteractable == false)
				return false;

			OnCloseButton();

			if (_closeButton != null)
			{
				_closeButton.PlayClickSound();
			}

			return true;
		}

		// PROTECTED METHODS

		protected virtual void OnCloseButton()
		{
			Close();

			if (BackView != null)
			{
				Open(BackView);
				BackView = null;
			}
		}
	}
}
