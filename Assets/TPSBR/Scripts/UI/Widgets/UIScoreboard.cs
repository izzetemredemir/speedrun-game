namespace TPSBR.UI
{
	using UnityEngine;
	using System.Collections.Generic;
	using TMPro;

	public class UIScoreboard : UIWidget
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private UIScoreboardItem _item;
		[SerializeField]
		private TextMeshProUGUI  _livesHeader;
		[SerializeField]
		private UIValue          _totalPlayers;

		[SerializeField]
		private RectTransform    _recordsGapSeparator;

		[SerializeField]
		private int              _maxShownRecords       = 10;
		[SerializeField]
		private int              _fixedFirstPlaces = 3;


		private ElementCache<UIScoreboardItem>           _items;
		private float                                    _refreshTimer;
		private static readonly PlayerStatisticsComparer _playerStatisticsComparer = new PlayerStatisticsComparer();

		// UIView INTERFAFCE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_items = new ElementCache<UIScoreboardItem>(_item, 16);
		}

		protected override void OnVisible()
		{
			base.OnVisible();

			Refresh();
		}

		protected override void OnTick()
		{
			base.OnTick();

			_refreshTimer -= Time.deltaTime;

			if (_refreshTimer > 0f)
				return;

			Refresh();
		}

		// PRIVATE METHODS

		private void Refresh()
		{
			if (Context.Runner == null || Context.Runner.Exists(Context.GameplayMode.Object) == false)
				return;

			var allStatistics       = ListPool.Get<PlayerStatistics>(200);
			var localPlayerPosition = 0;

			int playersCount = 0;

			foreach (var player in Context.NetworkGame.Players)
			{
				if (player == null)
					continue;

				var statistics = player.Statistics;

				allStatistics.Add(statistics);

				if (statistics.PlayerRef == Context.LocalPlayerRef)
				{
					localPlayerPosition = statistics.Position;
				}

				playersCount++;
			}

			_totalPlayers.SetValue(playersCount);

			allStatistics.Sort(_playerStatisticsComparer);

			var showLives = Context.GameplayMode is EliminationGameplayMode;
			_livesHeader.SetActive(showLives);

			if (localPlayerPosition <= _maxShownRecords)
			{
				var i = 0;
				var count = Mathf.Min(_maxShownRecords, allStatistics.Count);
				for (; i < count; i++)
				{
					var statistics = allStatistics[i];
					var player     = Context.NetworkGame.GetPlayer(statistics.PlayerRef);

					if (player != null)
					{
						_items[i].SetData(statistics, player.Nickname, showLives, Context.LocalPlayerRef == statistics.PlayerRef);
					}
				}

				_items.HideAll(i);

				_recordsGapSeparator.SetActive(allStatistics.Count > _maxShownRecords);
				_recordsGapSeparator.SetSiblingIndex(count + 2);
			}
			else
			{
				var i = 0;
				for (int count = _fixedFirstPlaces; i < count; i++)
				{
					var statistics = allStatistics[i];
					var player     = Context.NetworkGame.GetPlayer(statistics.PlayerRef);

					_items[i].SetData(statistics, player.Nickname, showLives, Context.LocalPlayerRef == statistics.PlayerRef);
				}

				_recordsGapSeparator.SetSiblingIndex(i + 2);
				_recordsGapSeparator.SetActive(true);

				var aroundPlayer      = _maxShownRecords - _fixedFirstPlaces;
				var secondsBlockStart = localPlayerPosition - aroundPlayer / 2 - 1;

				if (secondsBlockStart + aroundPlayer > allStatistics.Count)
				{
					secondsBlockStart -= secondsBlockStart + aroundPlayer - allStatistics.Count;
				}

				for (int y = secondsBlockStart; y < secondsBlockStart + aroundPlayer; i++, y++)
				{
					var statistics = allStatistics[y];
					var player     = Context.NetworkGame.GetPlayer(statistics.PlayerRef);

					_items[i].SetData(statistics, player.Nickname, showLives, Context.LocalPlayerRef == statistics.PlayerRef);
				}

				_items.HideAll(i);
			}


			ListPool.Return(allStatistics);

			_refreshTimer = 1f;
		}

		// HELPERS

		private class PlayerStatisticsComparer : IComparer<PlayerStatistics>
		{
			int IComparer<PlayerStatistics>.Compare(PlayerStatistics x, PlayerStatistics y)
			{
				return x.Position.CompareTo(y.Position);
			}
		}
	}
}
