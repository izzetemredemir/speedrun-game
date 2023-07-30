using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Solana.Unity.SDK;
using TMPro;
using UnityEngine;

namespace TPSBR
{
    public class DisplayBalance : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _txtBalance;

        // Start is called before the first frame update
        void Start()
        {
            _txtBalance = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            Web3.OnBalanceChange += OnBalanceChange;
        }

        private void OnDisable()
        {
            Web3.OnBalanceChange -= OnBalanceChange;
        }

        private void OnBalanceChange(double solAmount)
        {
            Debug.Log(solAmount);
            _txtBalance.text = "Balance: " + solAmount.ToString() + " SOL";
        }
    }
}
