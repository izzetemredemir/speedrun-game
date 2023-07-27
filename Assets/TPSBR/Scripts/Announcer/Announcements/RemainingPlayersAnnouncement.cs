using UnityEngine;

namespace TPSBR
{
	[CreateAssetMenu(menuName = "TPSBR/Announcements/Remaining Players")]
	public class RemainingPlayersAnnouncement : Announcement
	{
		// PRIVATE MEMBERS
		
		[SerializeField]
		private int _remainingPlayers;
		[SerializeField]
		private float _minNextAnnouncementTime = 30f;
		
		private int _lastActivePlayers;
		private float _lastAnnouncedTime;
		
		// Announcement INTERFACE

		protected override bool CheckCondition(AnnouncerContext context)
		{
			// Number of players could be increased during gameplay (new player join)
			// so we need to announce again after some time
			
			if (_lastActivePlayers > _remainingPlayers && context.ActivePlayers <= _remainingPlayers && _lastAnnouncedTime + _minNextAnnouncementTime < Time.timeSinceLevelLoad)
			{
				_lastAnnouncedTime = Time.timeSinceLevelLoad;
				return true;
			}
			
			_lastActivePlayers = context.ActivePlayers;
			
			return false;
		}
	}
}
