using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
	public enum EAnnouncementChannel
	{
		None,
		TimeRemaining,
		PlayersRemaining,
		KillsRemaining,
		KillsDone,
	}
	
	[Serializable]
	public struct AnnouncementData
	{
		public int                  Priority;
		public EAnnouncementChannel Channel;
		public string               TextMessage;
		public AudioSetup           AudioMessage;
		public float                Cooldown;
		public float                Duration;
		public float                ValidTime;
		public Color                Color;
		public string               FeedMessage;
		
		public float                ValidCooldown { get; set; }
	}
	
	public abstract class Announcement : ScriptableObject
	{
		// PUBLIC MEMBERS
		
		public bool IsFinished { get; set; }
		
		// PROTECTED MEMBERS
		
		[SerializeField]
		protected bool _oneTimeAnnouncement = true;
		[SerializeField]
		protected AnnouncementData _defaultSetup;

		// PUBLIC METHODS
		
		public virtual void Activate(AnnouncerContext context)
		{
			
		}
		
		public virtual void Tick(AnnouncerContext context, List<AnnouncementData> collectedAnnouncements)
		{
			if (CheckCondition(context) == true)
			{
				collectedAnnouncements.Add(_defaultSetup);
				
				if (_oneTimeAnnouncement == true)
				{
					IsFinished = true;
				}
			}
		}
		
		public virtual void Deactivate()
		{
			
		}
		
		// PROTECTED METHODS
		
		protected virtual bool CheckCondition(AnnouncerContext context)
		{
			return false;
		}
	}
}