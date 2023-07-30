using System;
using System.Collections;
using System.Collections.Generic;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR
{
    public class DisplayPublicKey : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _txtPublicKey;
        [SerializeField] private Button goToGameButton;

        // Start is called before the first frame update
        void Start()
        {
            goToGameButton.SetActive(false);
            _txtPublicKey = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            Web3.OnLogin += OnLogin;
        }

        private void OnDisable()
        {
            Web3.OnLogin -= OnLogin;
        }

        private void OnLogin(Account account)
        {
            _txtPublicKey.text = "Public Address: " + account.PublicKey;
            if (account.PublicKey != null)
            {
                goToGameButton.SetActive(true);
            }
        }
    }
}
