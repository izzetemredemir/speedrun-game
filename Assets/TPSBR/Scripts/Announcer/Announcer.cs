using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace TPSBR
{
	public class AnnouncerContext
	{
		public NetworkRunner Runner;
		public GameplayMode GameplayMode;

		public int ActivePlayers;
		public int BestScore;
		public PlayerStatistics PlayerStatistics;
	}

	public class Announcer : SceneService
	{
		// PUBLIC MEMBERS

		public Action<AnnouncementData> Announce;

		// PRIVATE MEMBERS

		[SerializeField]
		private AudioEffect _audio;

		private AnnouncerContext _context;
		private Announcement[] _announcements;

		private List<AnnouncementData> _collectedAnnouncements = new List<AnnouncementData>(32);
		private List<AnnouncementData> _waitingAnnouncements   = new List<AnnouncementData>(32);

		// SceneService INTERFACE

		protected override void OnTick()
		{
			if (ApplicationSettings.IsStrippedBatch == true)
				return;

			var gameplayMode = Context.GameplayMode;
			if (gameplayMode == null)
				return;

			if (Context.Runner == null || Context.Runner.Exists(gameplayMode.Object) == false)
				return;

			if (gameplayMode.State != GameplayMode.EState.Active)
				return;

			if (_context == null)
			{
				_context = new AnnouncerContext();

				_context.Runner = Context.Runner;
				_context.GameplayMode = gameplayMode;
			}

			PrepareContext();

			if (_announcements == null)
			{
				var announcementObjects = gameplayMode.Announcements;
				_announcements = new Announcement[announcementObjects.Length];

				for (int i = 0; i < announcementObjects.Length; i++)
				{
					var announcement = Instantiate(announcementObjects[i]);
					_announcements[i] = announcement;

					announcement.Activate(_context);
				}
			}

			UpdateAnnouncements();
		}

		protected override void OnDeactivate()
		{
			if (_announcements != null)
			{
				for (int i = 0; i < _announcements.Length; i++)
				{
					_announcements[i].Deactivate();
					Destroy(_announcements[i]);
				}

				_announcements = null;
			}

			_context = null;
		}

		protected override void OnDeinitialize()
		{
			Announce = null;
		}

		// PRIVATE METHODS

		private void PrepareContext()
		{
			_context.ActivePlayers = 0;
			_context.BestScore = 0;

			foreach (var player in Context.NetworkGame.Players)
			{
				if (player == null)
					continue;

				var statistics = player.Statistics;
				if (statistics.IsValid == false)
					continue;
				if (statistics.IsEliminated == true)
					continue;

				if (statistics.Score > _context.BestScore)
				{
					_context.BestScore = statistics.Score;
				}

				_context.ActivePlayers++;
			}

			var observedPlayer = Context.NetworkGame.GetPlayer(Context.ObservedPlayerRef);
			_context.PlayerStatistics = observedPlayer != null ? observedPlayer.Statistics : default;
		}

		private void UpdateAnnouncements()
		{
			float deltaTime = Time.deltaTime;

			// Collect
			for (int i = 0; i < _announcements.Length; i++)
			{
				Announcement announcement = _announcements[i];

				if (announcement.IsFinished == true)
					continue;

				announcement.Tick(_context, _collectedAnnouncements);
			}

			// Add
			if (_collectedAnnouncements.Count > 0)
			{
				AddAnnouncements(_collectedAnnouncements);
				_collectedAnnouncements.Clear();
			}

			// Update cooldowns
			for (int i = _waitingAnnouncements.Count - 1; i >= 0; i--)
			{
				AnnouncementData announcement = _waitingAnnouncements[i];

				if (announcement.Cooldown > 0f)
				{
					announcement.Cooldown -= deltaTime;
					_waitingAnnouncements[i] = announcement;
				}
			}

			// Try announce
			if (_waitingAnnouncements.Count > 0 && TryAnnounce(_waitingAnnouncements[0]) == true)
			{
				_waitingAnnouncements.RemoveAt(0);
			}

			// Update validity
			for (int i = _waitingAnnouncements.Count - 1; i >= 0; i--)
			{
				AnnouncementData announcement = _waitingAnnouncements[i];

				if (announcement.ValidCooldown > 0f)
				{
					announcement.ValidCooldown -= deltaTime;
					_waitingAnnouncements[i] = announcement;
				}
				else
				{
					_waitingAnnouncements.RemoveAt(i);
				}
			}
		}

		private void AddAnnouncements(List<AnnouncementData> newAnnouncements)
		{
			if (newAnnouncements.Count == 0)
				return;

			for (int i = 0; i < newAnnouncements.Count; i++)
			{
				AnnouncementData newAnnouncement = newAnnouncements[i];
				newAnnouncement.ValidCooldown = newAnnouncement.ValidTime;

				bool add = true;

				for (int j = 0; j < _waitingAnnouncements.Count; j++)
				{
					if (_waitingAnnouncements[j].Channel == newAnnouncement.Channel)
					{
						if (_waitingAnnouncements[j].Priority <= newAnnouncement.Priority)
						{
							_waitingAnnouncements[j] = newAnnouncement;
						}

						add = false;
					}
				}

				if (add == true)
				{
					_waitingAnnouncements.Add(newAnnouncement);
				}
			}

			_waitingAnnouncements.Sort((a, b) => b.Priority.CompareTo(a.Priority));
		}

		private bool TryAnnounce(AnnouncementData announcement)
		{
			if (announcement.Cooldown > 0f)
				return false;

			if (announcement.AudioMessage != null && announcement.AudioMessage.Clips.Length > 0)
			{
				if (_audio.IsPlaying == true)
					return false;

				_audio.Play(announcement.AudioMessage);
			}

			Announce?.Invoke(announcement);

			return true;
		}
	}
}
