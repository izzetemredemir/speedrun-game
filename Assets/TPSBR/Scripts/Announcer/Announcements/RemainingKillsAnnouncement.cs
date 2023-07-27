using UnityEngine;

namespace TPSBR
{
	[CreateAssetMenu(menuName = "TPSBR/Announcements/Remaining Kills")]
	public class RemainingKillsAnnouncement : Announcement
	{
		// PRIVATE MEMBERS
		
		[SerializeField]
		private int _kills;
		
		private DeathmatchGameplayMode _deathmatch;
		private int _minScore;
		
		// Announcement INTERFACE

		public override void Activate(AnnouncerContext context)
		{
			_deathmatch = context.GameplayMode as DeathmatchGameplayMode;
			
			if (_deathmatch == null)
			{
				// We do not need this announcement in other gameplay modes
				IsFinished = true;
				return;
			}
			
			_minScore = _deathmatch.ScoreLimit - _deathmatch.ScorePerKill * _kills;
			
			if (context.BestScore >= _minScore)
			{
				// Do not consider this announcement
				IsFinished = true;
			}
		}

		protected override bool CheckCondition(AnnouncerContext context)
		{
			return context.BestScore >= _minScore;
		}
	}
}