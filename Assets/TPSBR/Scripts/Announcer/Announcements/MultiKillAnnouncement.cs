using Fusion;
using UnityEngine;

namespace TPSBR
{
	[CreateAssetMenu(menuName = "TPSBR/Announcements/Multi Kill")]
	public class MultiKillAnnouncement : Announcement
	{
		// PRIVATE MEMBERS
		
		[SerializeField]
		private int _kills = 2;
		[SerializeField]
		private bool _inRowOnly = true;

		private PlayerRef _lastPlayer;
		private int _lastKills;
		
		// Announcement INTERFACE
		
		protected override bool CheckCondition(AnnouncerContext context)
		{
			// Player could change (e.g. when spectating)
			if (_lastPlayer != context.PlayerStatistics.PlayerRef)
			{
				_lastPlayer = context.PlayerStatistics.PlayerRef;
				_lastKills = context.PlayerStatistics.Kills;
				return false;
			}
			
			int lastKills = _lastKills;
			_lastKills = context.PlayerStatistics.Kills;
			
			if (context.PlayerStatistics.Kills > lastKills)
			{
				int currentKills = _inRowOnly == true ? context.PlayerStatistics.KillsInRow : context.PlayerStatistics.KillsWithoutDeath;
				
				if (currentKills == _kills)
					return true;
				
				if (_inRowOnly == false && currentKills > _kills && currentKills % _kills == 0)
					return true;
			}
			
			return false;
		}
	}
}