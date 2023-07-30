using System;
using System.Collections;
using System.Collections.Generic;
using codebase.utility;
using Solana.Unity.SDK;
using Solana.Unity.Wallet.Bip39;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace TPSBR
{
    public class GenerateAccount : MonoBehaviour
{
    [SerializeField]
    public TextMeshProUGUI mnemonicTxt;
    [SerializeField]
    public Button generateBtn;
    [SerializeField]
    public Button restoreBtn;
    [SerializeField]
    public Button saveMnemonicsBtn;
    [SerializeField]
    public Button backBtn;
    [SerializeField]
    public TMP_InputField passwordInputField;
    [SerializeField]
    public TextMeshProUGUI needPasswordTxt;

    public GameObject regenerateScreen;
    private void Start()
    {
        mnemonicTxt.text = new Mnemonic(WordList.English, WordCount.TwentyFour).ToString();
        
        if(generateBtn != null)
        {
            generateBtn.onClick.AddListener(() =>
            {
                MainThreadDispatcher.Instance().Enqueue(GenerateNewAccount);
            });
        }

        if(restoreBtn != null)
        {
            restoreBtn.onClick.AddListener(() =>
            {
                
                if(regenerateScreen != null)
                {
                    this.gameObject.SetActive(false);
                    regenerateScreen.SetActive(true);
                }
                else
                {
                    Debug.LogError("re-generate_screen GameObject not found. Make sure the name is correct and the GameObject is active in the scene.");
                }
            });
        }

        if(saveMnemonicsBtn != null)
        {
            saveMnemonicsBtn.onClick.AddListener(CopyMnemonicsToClipboard);
        }

        if(backBtn != null)
        {
            backBtn.onClick.AddListener(() =>
            {
                // Add your logic here
            });
        }
    }

    private void OnEnable()
    {
        needPasswordTxt.gameObject.SetActive(false);
        mnemonicTxt.text = new Mnemonic(WordList.English, WordCount.TwentyFour).ToString();
    }

    private async void GenerateNewAccount()
    {
        if (string.IsNullOrEmpty(passwordInputField.text))
        {
            needPasswordTxt.gameObject.SetActive(true);
            needPasswordTxt.text = "Need Password!";
            return;
        }
        
        var password = passwordInputField.text;
        var mnemonic = mnemonicTxt.text.Trim();
        try
        {
            await Web3.Instance.CreateAccount(mnemonic, password);
            needPasswordTxt.gameObject.SetActive(false);
        }
        catch (Exception ex)
        {
            passwordInputField.gameObject.SetActive(true);
            passwordInputField.text = ex.ToString();
        }
    }
    
    public void CopyMnemonicsToClipboard()
    {
        Clipboard.Copy(mnemonicTxt.text.Trim());
        gameObject.GetComponent<Toast>()?.ShowToast("Mnemonics copied to clipboard", 3);
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
