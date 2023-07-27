namespace TPSBR
{
	using System.Collections.Generic;

	public class DeathmatchGameplayMode : GameplayMode
	{
		// PUBLIC MEMBERS

		public int   ScoreLimit;

		// PRIVATE MEMBERS

		private DeathmatchComparer _playerComparer = new DeathmatchComparer();

		// GameplayMode INTERFACE

		protected override void AgentDeath(ref PlayerStatistics victimStatistics, ref PlayerStatistics killerStatistics)
		{
			base.AgentDeath(ref victimStatistics, ref killerStatistics);

			if (killerStatistics.IsValid == true && victimStatistics.PlayerRef != killerStatistics.PlayerRef)
			{
				if (killerStatistics.Score >= ScoreLimit)
				{
					FinishGameplay();
				}
			}
		}

		protected override void SortPlayers(List<PlayerStatistics> allStatistics)
		{
			allStatistics.Sort(_playerComparer);
		}

		protected override void CheckWinCondition()
		{
			foreach (var player in Context.NetworkGame.Players)
			{
				if (player == null)
					continue;

				if (player.Statistics.Score >= ScoreLimit)
				{
					FinishGameplay();
					return;
				}
			}
		}

		// HELPERS

		private class DeathmatchComparer : IComparer<PlayerStatistics>
		{
			public int Compare(PlayerStatistics x, PlayerStatistics y)
			{
				var result = y.Score.CompareTo(x.Score);
				if (result != 0)
					return result;

				result = y.Kills.CompareTo(x.Kills);
				if (result != 0)
					return result;

				return x.Deaths.CompareTo(y.Deaths);
			}
		}
	}
}
