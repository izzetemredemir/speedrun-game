using UnityEngine;
using System;
using TMPro;

namespace TPSBR.UI
{
	public class UIYesNoDialogView : UIDialogView 
	{
		// PUBLIC MEMBERS

		public bool                   Result        { get; private set; }
		public new event Action<bool> HasClosed;

		public UIButton               YesButton;
		public TextMeshProUGUI        YesButtonText;
		public UIButton               NoButton;
		public TextMeshProUGUI        NoButtonText;

		// PRIVATE MEMBERS

		[SerializeField]
		private string _defaultYesButtonText = "Confirm";
		[SerializeField]
		private string _defaultNoButtonText  = "Cancel";

		// PUBLIC METHODS

		public override void Clear()
		{
			base.Clear();

			YesButtonText.SetTextSafe(_defaultYesButtonText);
			NoButtonText.SetTextSafe(_defaultNoButtonText);
		}

		// UIView INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			YesButton.onClick.AddListener(OnYesButton);
			NoButton.onClick.AddListener(OnNoButton);
		}

		protected override void OnDeinitialize()
		{
			YesButton.onClick.RemoveListener(OnYesButton);
			NoButton.onClick.RemoveListener(OnNoButton);

			base.OnDeinitialize();
		}

		protected override void OnOpen()
		{
			base.OnOpen();

			Result = false;
		}

		protected override void OnClose()
		{
			base.OnClose();

			if (HasClosed != null)
			{
				HasClosed.Invoke(Result);
				HasClosed = null;
			}
		}

		// PRIVATE METHODS

		private void OnYesButton()
		{
			Result = true;
			Close();
		}

		private void OnNoButton()
		{
			Result = false;
			Close();
		}
	}
}
