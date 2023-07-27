using UnityEngine;
using TMPro;

namespace TPSBR.UI
{
	public class UIChangeNicknameView : UICloseView
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private TextMeshProUGUI _caption;
		[SerializeField]
		private TMP_InputField _name;
		[SerializeField]
		private UIButton _confirmButton;
		[SerializeField]
		private int _minCharacters = 5;

		// PUBLIC METHODS

		public void SetData(string caption, bool nameRequired)
		{
			_caption.text = caption;
			CloseButton.SetActive(nameRequired == false);
		}

		// UIView INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_confirmButton.onClick.AddListener(OnConfirmButton);
		}

		protected override void OnDeinitialize()
		{
			_confirmButton.onClick.RemoveListener(OnConfirmButton);

			base.OnDeinitialize();
		}

		protected override void OnOpen()
		{
			base.OnOpen();

			string currentNickname = Context.PlayerData.Nickname;
			if (currentNickname.HasValue() == false)
			{
				_name.text = "Player" + Random.Range(10000, 100000);
			}
			else
			{
				_name.text = Context.PlayerData.Nickname;
			}
		}

		protected override void OnTick()
		{
			base.OnTick();

			_confirmButton.interactable = _name.text.Length >= _minCharacters && _name.text != Context.PlayerData.Nickname;
		}

		// PRIVATE METHODS

		private void OnConfirmButton()
		{
			Context.PlayerData.Nickname = _name.text;

			Close();
		}
	}
}
