using UnityEngine;
using TMPro;
using System.Collections;

namespace TPSBR.UI
{
    public class UIChangeAvatarUrlView : UICloseView
	{
        // PRIVATE MEMBERS

        [SerializeField]
		private TextMeshProUGUI _caption;
		[SerializeField]
		private TMP_InputField _avatarUrlField;
		[SerializeField]
		private UIButton _confirmButton;
        [SerializeField]
        private UIButton _createButton;
        [SerializeField]
        private TextMeshProUGUI _createButtonText;
        [SerializeField]
        private GameObject _avatarLoader;

        private bool hasOpenBrowser;

		// PUBLIC METHODS

		public void SetData(string caption, bool avatarRequired)
		{
			_caption.text = caption;
			CloseButton.SetActive(avatarRequired == false);
		}

		// UIView INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

            _avatarUrlField.interactable = false;
            _confirmButton.onClick.AddListener(OnConfirmButton);
            _createButton.onClick.AddListener(OnCreateButtonClicked);
        }

		protected override void OnDeinitialize()
		{
			_confirmButton.onClick.RemoveListener(OnConfirmButton);
            _createButton.onClick.RemoveListener(OnCreateButtonClicked);

            base.OnDeinitialize();
		}

		protected override void OnOpen()
		{
			base.OnOpen();

			string currentAvatarurl = Context.PlayerData.AvatarUrl;
			if (currentAvatarurl.HasValue() == false)
			{
                _avatarUrlField.text = string.Empty;
			}
			else
			{
                _avatarUrlField.text = Context.PlayerData.AvatarUrl;
			}
		}

		protected override void OnTick()
        {
            base.OnTick();

            string avatarUrl = _avatarUrlField.text;

            bool isNew = avatarUrl != Context.PlayerData.AvatarUrl && avatarUrl.Length == RPMPlayerSetting.Data.runtimeAvatarUrlLength;
            bool isUrlValid = avatarUrl.StartsWith(RPMPlayerSetting.Data.validationStart) && (avatarUrl.Contains(RPMPlayerSetting.Data.validationContain) || avatarUrl.EndsWith(RPMPlayerSetting.Data.validationEnd));

            _confirmButton.interactable = isNew && isUrlValid;

            SetPasteOrClearTextUI();
        }

		// PRIVATE METHODS

		private void OnConfirmButton()
		{
            Context.PlayerData.AvatarUrl = _avatarUrlField.text;
            _avatarLoader.GetComponent<SimpleAvatarLoaderMenu>().OnSetRuntimeAvatar(Context.PlayerData.AvatarUrl);

            Close();
		}
        private void OnCreateButtonClicked()
        {
            SetPasteOrClearAction();
        }
        private void SetPasteOrClearTextUI()
        {
            string avatarUrl = _avatarUrlField.text;

            if (string.IsNullOrEmpty(avatarUrl))
            {
                if (hasOpenBrowser)
                {
                    _createButtonText.text = RPMConstant.PASTE;
                }
                else
                {
                    _createButtonText.text = RPMConstant.CREATE;
                }
            }
            else
            {
                _createButtonText.text = RPMConstant.CLEAR;
            }
        }
        private void SetPasteOrClearAction()
        {
            string avatarUrl = _avatarUrlField.text;

            if (string.IsNullOrEmpty(avatarUrl))
            {
                if (hasOpenBrowser)
                {
                    StartCoroutine(PasteCoroutine());
                }
                else
                {
                    StartCoroutine(OpenBrowserCoroutine());
                }
            }
            else
            {
                _avatarUrlField.text = string.Empty;
            }
        }
        private IEnumerator OpenBrowserCoroutine()
        {
            if (hasOpenBrowser)
            {
                yield break;
            }

            SimpleOpenBrowser.OpenBrowser(RPMPlayerSetting.Data.openUrl);
            _avatarUrlField.text = string.Empty;

            yield return new WaitForSeconds(1);

            hasOpenBrowser = true;
        }
        private IEnumerator PasteCoroutine()
        {
            if (!hasOpenBrowser)
            {
                yield break;
            }

            string clipboard = SimpleGetClipboard.GetClipboardText();
            _avatarUrlField.SetTextWithoutNotify(clipboard);

            yield return new WaitForSeconds(1);

            hasOpenBrowser = false;
        }
    }
}