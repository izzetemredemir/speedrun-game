using UnityEngine;

namespace TPSBR
{
	[CreateAssetMenu(menuName = "TPSBR/Announcements/Remaining Time")]
	public class RemainingTimeAnnouncement : Announcement
	{
		// PRIVATE MEMBERS
		
		[SerializeField]
		private float _remainingTime = 60f;
		
		// Announcement INTERFACE

		public override void Activate(AnnouncerContext context)
		{
			base.Activate(context);
			
			if (context.GameplayMode.RemainingTime <= _remainingTime)
			{
				// Do not consider this announcement
				IsFinished = true;
			}
		}

		protected override bool CheckCondition(AnnouncerContext context)
		{
			return context.GameplayMode.RemainingTime <= _remainingTime;
		}
	}
}
