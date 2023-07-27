namespace TPSBR.UI
{
	using UnityEngine;
	using TMPro;

	public class UIGameplayPanel : UIWidget
	{
		[SerializeField]
		private TextMeshProUGUI _mode;
		[SerializeField]
		private TextMeshProUGUI _time;
		[SerializeField]
		private TextMeshProUGUI _leftCaption;
		[SerializeField]
		private TextMeshProUGUI _leftText;
		[SerializeField]
		private TextMeshProUGUI _rightCaption;
		[SerializeField]
		private TextMeshProUGUI _rightText;

		private int _lastSeconds;
		private bool _isElimination;

		private float _refreshCooldown;

		// UIWidget INTERFACE

		protected override void OnVisible()
		{
			base.OnVisible();

			_mode.text = Context.GameplayMode.GameplayName;
			_isElimination = Context.GameplayMode is EliminationGameplayMode;

			_rightCaption.text = _isElimination == true ? "Lives" : "Score";
		}

		protected override void OnTick()
		{
			base.OnTick();

			if (Context.Runner == null || Context.Runner.Exists(Context.GameplayMode.Object) == false)
				return;

			_refreshCooldown -= Time.deltaTime;

			if (_refreshCooldown <= 0f)
			{
				_refreshCooldown = 1f;

				Refresh();
			}

			int remainingSeconds = Mathf.CeilToInt(Context.GameplayMode.RemainingTime);
			if (remainingSeconds > 0)
			{
				if (_lastSeconds != remainingSeconds)
				{
					_time.text = $"{remainingSeconds / 60}:{remainingSeconds % 60 :00}";

					_lastSeconds = remainingSeconds;
				}
			}
			else
			{
				_time.text = "∞";
			}
		}

		// PRIVATE METHODS

		private void Refresh()
		{
			var localPlayer = Context.NetworkGame.GetPlayer(Context.LocalPlayerRef);
			var statistics = localPlayer != null ? localPlayer.Statistics : default;

			_leftText.text = statistics.Position > 0 ? $"#{statistics.Position}" : "~";
			_rightText.text = _isElimination == true ? statistics.ExtraLives.ToString() : statistics.Score.ToString();
		}
	}
}
