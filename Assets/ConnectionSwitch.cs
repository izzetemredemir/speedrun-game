using System;
using System.Collections;
using System.Collections.Generic;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR
{
    public class ConnectionSwitch : MonoBehaviour
    {
        [SerializeField] private Button btnConnect;
        [SerializeField] private Button btnDisconnect;
        [SerializeField] private GameObject txtPublicKey;
        [SerializeField] private GameObject txtBalance;

        private void Start()
        {
            btnDisconnect.onClick.AddListener(() => Web3.Instance.Logout());
        }

        private void OnEnable()
        {
            Web3.OnLogin += OnLogin;
            Web3.OnLogout += OnLogout;
        }

        private void OnDisable()
        {
            Web3.OnLogin -= OnLogin;
            Web3.OnLogout -= OnLogout;
        }

        private void OnLogin(Account obj)
        {
            btnConnect.gameObject.SetActive(false);
            btnDisconnect.gameObject.SetActive(true);
            txtPublicKey.gameObject.SetActive(true);
            txtBalance.gameObject.SetActive(true);
        }
        
        private void OnLogout()
        {
            btnConnect.gameObject.SetActive(true);
            btnDisconnect.gameObject.SetActive(false);
            txtPublicKey.gameObject.SetActive(false);
            txtBalance.gameObject.SetActive(false);
        }
    }
}
