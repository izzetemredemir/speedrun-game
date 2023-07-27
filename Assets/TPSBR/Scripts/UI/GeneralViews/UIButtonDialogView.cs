using TMPro;

namespace TPSBR.UI
{
	public class UIButtonDialogView : UIDialogView
	{
		// PUBLIC MEMBERS

		public UIButton        ConfirmButton;
		public TextMeshProUGUI ConfirmButtonText;

		// PRIVATE MEMBERS

		private string _defaultOkButtonText;

		// PUBLIC METHODS

		public override void Clear()
		{
			base.Clear();

			ConfirmButtonText.SetTextSafe(_defaultOkButtonText);
		}

		// UIView INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			ConfirmButton.onClick.AddListener(OnConfirmButton);

			_defaultOkButtonText = ConfirmButtonText.GetTextSafe();
		}

		protected override void OnDeinitialize()
		{
			ConfirmButton.onClick.RemoveListener(OnConfirmButton);

			base.OnDeinitialize();
		}

		// PRIVATE METHODS

		private void OnConfirmButton()
		{
			Close();
		}
	}
}
