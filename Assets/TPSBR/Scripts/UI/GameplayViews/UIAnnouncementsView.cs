using UnityEngine;

namespace TPSBR.UI
{
	public class UIAnnouncementsView : UIView 
	{
		// PRIVATE MEMBERS
		
		[SerializeField]
		private UIBehaviour _importantAnnouncement;
		[SerializeField]
		private UIBehaviour _killAnnouncement;

		// UIView INTERFACE

		protected override void OnOpen()
		{
			Context.Announcer.Announce += OnAnnounce;
			
			_importantAnnouncement.SetActive(false);
			_killAnnouncement.SetActive(false);
		}

		protected override void OnClose()
		{
			Context.Announcer.Announce -= OnAnnounce;
		}
		
		// PRIVATE METHODS
		
		private void OnAnnounce(AnnouncementData announcement)
		{
			if (announcement.TextMessage.HasValue() == false)
				return;
		
			var item = announcement.Channel == EAnnouncementChannel.KillsDone ? _killAnnouncement : _importantAnnouncement;
			
			item.gameObject.SetActive(false);
			item.Text.text = announcement.TextMessage;
			item.Text.color = announcement.Color;
			item.gameObject.SetActive(true);
		}
	}
}
