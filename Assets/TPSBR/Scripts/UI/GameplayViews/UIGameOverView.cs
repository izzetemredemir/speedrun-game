using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
	public class UIGameOverView : UIView
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private UIBehaviour _winnerGroup;
		[SerializeField]
		private TextMeshProUGUI _winner;
		[SerializeField]
		private UIButton _restartButton;
		[SerializeField]
		private AudioSetup _openSound;

		// UIView INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_restartButton.onClick.AddListener(OnRestartButton);
		}

		protected override void OnOpen()
		{
			base.OnOpen();

			var winnerStatistics = GetWinner();
			Player winner = null;

			if (winnerStatistics.IsValid == true)
			{
				winner = Context.NetworkGame.GetPlayer(winnerStatistics.PlayerRef);
			}

			if (winner != null)
			{
				_winner.text = $"Winner is {winner.Nickname}";
				_winnerGroup.SetActive(true);
			}
			else
			{
				_winnerGroup.SetActive(false);
			}

			PlaySound(_openSound);

			Global.Networking.StopGameOnDisconnect();
		}

		protected override void OnDeinitialize()
		{
			_restartButton.onClick.RemoveListener(OnRestartButton);

			base.OnDeinitialize();
		}

		// PRIVATE MEMBERS

		private PlayerStatistics GetWinner()
		{
			foreach (var player in Context.NetworkGame.Players)
			{
				if (player == null)
					continue;

				var statistics = player.Statistics;
				if (statistics.Position == 1)
				{
					return statistics;
				}
			}

			return default;
		}

		private void OnRestartButton()
		{
			Global.Networking.StopGame();
		}
	}
}
