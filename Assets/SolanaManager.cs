using Solana.Unity.SDK;
using Solana.Unity.Rpc;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR
{


    public class SolanaManager : MonoBehaviour
    {/*
        // UI elements
        public Button registerButton;
        public Button checkScoreButton;
        public Button upgradeButton;
        public Text transactionHashText;

        // The public key of the smart contract
        private string smartContractPublicKey = "YourSmartContractPublicKey";

        // The public key of the player
        private string playerPublicKey = "PlayerPublicKey";

        // The score of the player
        private int score = 100;

        // The instance of the custom Web3 class
        private CustomWeb3 customWeb3;

        private void Start()
        {
            // Initialize the custom Web3 instance
            customWeb3 = new CustomWeb3();

            // Set up button click events
            registerButton.onClick.AddListener(async () => await RegisterPlayer());
            checkScoreButton.onClick.AddListener(async () => await CheckScore());
            upgradeButton.onClick.AddListener(async () => await UpgradePlayer());
        }

        // Method to register a player
        public async Task RegisterPlayer()
        {
            // Register the player and get the transaction hash
            string transactionHash = await customWeb3.RegisterPlayer(playerPublicKey, score);

            // Display the transaction hash
            transactionHashText.text = "Transaction Hash: " + transactionHash;
        }

        // Method to check the score of a player
        public async Task CheckScore()
        {
            // Get the account info of the player
            var accountInfo = await customWeb3.GetAccountInfo(playerPublicKey);

            // Check if the player's score is 500 or more
            if (accountInfo.Value >= 500)
            {
                // Disable the check score button and enable the upgrade button
                checkScoreButton.gameObject.SetActive(false);
                upgradeButton.gameObject.SetActive(true);
            }
        }

        // Method to upgrade a player
        public async Task UpgradePlayer()
        {
            // Deduct the score from the player and get the transaction hash
            string transactionHash = await customWeb3.RegisterPlayer(playerPublicKey, -500);

            // Display the transaction hash
            transactionHashText.text = "Transaction Hash: " + transactionHash;

            // Run your custom logic here
        }
    }

    public class CustomWeb3 : Web3
    {
        // Method to register a player
        public async Task<string> RegisterPlayer(string playerPublicKey, int score)
        {
            // Create the instruction for the smart contract
            var instruction = SystemProgram.Transfer(playerPublicKey, smartContractPublicKey, score);

            // Send the transaction and get the signature
            var signature = await Wallet.SendTransaction(new List<Instruction> { instruction });

            // Return the transaction hash
            return signature;
        }*/

    }
}