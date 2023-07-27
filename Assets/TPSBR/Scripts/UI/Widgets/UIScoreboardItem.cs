namespace TPSBR.UI
{
	using UnityEngine;
	using TMPro;

	public class UIScoreboardItem : UIBehaviour
	{
		[SerializeField]
		private TextMeshProUGUI _position;
		[SerializeField]
		private TextMeshProUGUI _nickname;
		[SerializeField]
		private TextMeshProUGUI _kills;
		[SerializeField]
		private TextMeshProUGUI _deaths;
		[SerializeField]
		private TextMeshProUGUI _score;
		[SerializeField]
		private TextMeshProUGUI _lives;
		[SerializeField]
		private CanvasGroup _deadIcon;

		[SerializeField]
		private CanvasGroup      _normalBackground;
		[SerializeField]
		private CanvasGroup      _localPlayerBackground;

		public void SetData(PlayerStatistics statistics, string nickname, bool showLives, bool isLocal)
		{
			_position.text = $"#{statistics.Position}";
			_nickname.text = nickname;
			_kills.text    = statistics.Kills.ToString("N0");
			_deaths.text   = statistics.Deaths.ToString("N0");
			_score.text    = statistics.Score.ToString("N0");

			if (showLives == true)
			{
				_lives.SetActive(true);
				_lives.text = statistics.IsEliminated == false ? statistics.ExtraLives.ToString() : "Eliminated";
			}
			else
			{
				_lives.SetActive(false);
			}

			_deadIcon.SetVisibility(statistics.IsAlive == false);

			_normalBackground.SetVisibility(isLocal == false);
			_localPlayerBackground.SetVisibility(isLocal == true);
		}
	}
}
