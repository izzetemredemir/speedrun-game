using UnityEngine;
using System.Collections.Generic;
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

        // EDITED
        [SerializeField]
        private TMP_Dropdown _genderDropdown; 
		[SerializeField]
		private GameObject _avatarLoader;
		// END

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

            // EDITED
            string[] options = new string[] { "SELECT", "MALE", "FEMALE" };
            _genderDropdown.AddOptions(new List<string>(options));
			// END

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

            // EDITED
            int currentGender = Context.PlayerData.Gender;
            if (currentGender > 0)
            {
                _genderDropdown.value = currentGender;
            }
			else
			{
				_genderDropdown.value = 0;
			}
			// END
        }

		protected override void OnTick()
		{
			base.OnTick();

            // EDITED
            // _confirmButton.interactable =  _name.text.Length >= _minCharacters && _name.text != Context.PlayerData.Nickname; 
            bool IsNameValid = _name.text.Length >= _minCharacters && _name.text != Context.PlayerData.Nickname; 
			bool IsGenderSelected = _genderDropdown.value != 0;

            _confirmButton.interactable = IsNameValid && IsGenderSelected;
			// END
		}

        // PRIVATE METHODS

        private void OnConfirmButton()
        {
			Context.PlayerData.Nickname = _name.text;

            // EDITED
            Context.PlayerData.Gender = _genderDropdown.value;
			_avatarLoader.GetComponent<SimpleAvatarLoaderMenu>().OnSetGender(Context.PlayerData.Gender);
			// END

            Close();
		}
	}
}