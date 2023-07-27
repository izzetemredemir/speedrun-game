using TMPro;

namespace TPSBR.UI
{
	public class UIDialogView : UICloseView 
	{
		// PUBLIC MEMBERS

		public TextMeshProUGUI Title;
		public TextMeshProUGUI Description;

		// PRIVATE MEMBERS

		private string _defaultTitleText;
		private string _defaultDescriptionText;

		// PUBLIC METHODS

		public virtual void Clear()
		{
			Title.SetTextSafe(_defaultTitleText);
			Description.SetTextSafe(_defaultDescriptionText);
		}

		// UIView INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_defaultTitleText = Title.GetTextSafe();
			_defaultDescriptionText = Description.GetTextSafe();
		}

		protected override void OnClose()
		{
			base.OnClose();

			Clear();
		}
	}
}
