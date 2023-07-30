using System.Collections;
using System.Collections.Generic;
using Solana.Unity.SDK;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR
{
    public class ReGenerateAccount : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField mnemonicTxt;
        [SerializeField]
        private Button generateBtn;
        [SerializeField]
        private Button createBtn;
        [SerializeField]
        private Button backBtn;
        [SerializeField]
        private Button loadMnemonicsBtn;
        [SerializeField]
        private TMP_InputField passwordInputField;
        [SerializeField]
        private TextMeshProUGUI wrongPasswordTxt;
        [SerializeField]
        private TextMeshProUGUI errorTxt;

        private void OnEnable()
        {
            wrongPasswordTxt.gameObject.SetActive(false);
        }

        private void Start()
        {
            if(generateBtn != null)
            {
                generateBtn.onClick.AddListener(GenerateNewAccount);
            }

            if(createBtn != null)
            {
                createBtn.onClick.AddListener(() =>
                {
                    // Add your logic here
                });
            }

            if(backBtn != null)
            {
                backBtn.onClick.AddListener(() =>
                {
                    // Add your logic here
                });
            }

            loadMnemonicsBtn.onClick.AddListener(PasteMnemonicsClicked);
        }

        private async void GenerateNewAccount()
        {
            var password = passwordInputField.text;
            var mnemonic = mnemonicTxt.text;

            var account = await Web3.Instance.CreateAccount(mnemonic, password);
            if (account != null)
            {
                // Add your logic here
            }
            else
            {
                errorTxt.text = "Keywords are not in a valid format.";
            }
        }
    
        private void PasteMnemonicsClicked()
        {
            mnemonicTxt.text = GUIUtility.systemCopyBuffer;
        }

        public void OnClose()
        {
            var wallet = GameObject.Find("wallet");
            if (wallet != null)
            {
                wallet.SetActive(false);
            }
        }
    }
    
}
