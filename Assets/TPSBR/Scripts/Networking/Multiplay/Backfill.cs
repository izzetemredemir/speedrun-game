#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
namespace TPSBR
{
	/// <summary>
	/// Backfill works only on server
	/// </summary>
	public sealed class Backfill : SceneService
	{
		public bool  BackfillEnabled { get; set; }
		public float PlayerJoiningExpiration = 20;

		public void PlayerJoined(Player player) {}
		public void PlayerLeft(Player player)   {}
	}
}
#else
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

namespace TPSBR
{
	/// <summary>
	/// Uses Unity matchmaker to backfill more players into a Multiplay running dedicated game server
	/// </summary>
	public sealed class Backfill : SceneService
	{
		// PUBLIC MEMBERS

		public bool BackfillEnabled
		{
			get => _backfillEnabled;
			set
			{
				if (_isBackfillingValid) _backfillEnabled = value;
			}
		}

		public float PlayerJoiningExpiration = 20;

		// PRIVATE MEMBERS

		private bool _pendingBackfillChange = false;
		private BackfillTicket _backfillTicket;
		private bool _isBackfillingValid => Global.MultiplayManager != null && Global.MultiplayManager.Backfill;
		private bool _backfillEnabled;

		private string _connection;
		private string _region;
		private string _everyoneTeamName = "everyone";
		private string _everyoneTeamID;
		private string _queueName;

		/// <summary>
		/// Keeps track of players that are expected to connect through matchmaking
		/// along with when they connected so they can be removed if they don't arrive after a period of time
		/// </summary>
		private Dictionary<string, float> _playersMatchingIn = new();

		// Note: NetworkGame.Players is a NetworkArray of length = max players, filling from the last position
		// This simplifies access to the current actively connected players
		private IEnumerable<Player> _networkPlayers => Context.NetworkGame.Players.Where(p => p != null);

		private float _backfillIntervalMs = 3.0f;
		private float _backfillTimerMs = 0f;

		// PUBLIC METHODS

		public void PlayerJoined(Player player)
		{
			if (player.UnityID.HasValue() == false)
			{
				Debug.Log($"Player [{player.UserID}] doesn't have UnityID, skipping...");
				return;
			}

			Debug.Log($"Player [{player.UserID}] joined with UnityID [{player.UnityID}]");

			if (_playersMatchingIn.ContainsKey(player.UnityID))
			{
				// The player was expected, remove them from the "matching in" list
				_playersMatchingIn.Remove(player.UnityID);
			}
			else if(_backfillTicket != null)
			{
				// The player came from outside matchmaker, we need to update the backfill ticket
				_pendingBackfillChange = true;
			}
		}

		public void PlayerLeft(Player player)
		{
			if (player.UnityID.HasValue() == false)
				return;

			Debug.Log($"Player [{player.UserID}] with UnityID [{player.UnityID}] left");

			if (_backfillTicket != null)
			{
				// A player left, we need to update the backfill ticket
				_pendingBackfillChange = true;
			}
		}

		// SceneService INTERFACE

		protected override async void OnInitialize()
		{
			base.OnInitialize();

			_queueName = Global.Settings.Network.GetCustomOrDefaultQueueName();

			// If there are matchmaking hints, we'll take them
			MatchmakingResults initMmResults = null;
			if (Global.MultiplayManager != null && Global.MultiplayManager.MatchmakingResults != null)
			{
				initMmResults = Global.MultiplayManager.MatchmakingResults;
				_queueName = initMmResults.QueueName;
			}

			if(_isBackfillingValid && !string.IsNullOrEmpty(initMmResults?.BackfillTicketId)){
				// Take note of which players we're expecting and enable backfill
				Debug.Log("Initializing backfill");
				_backfillTicket = await MatchmakerService.Instance.ApproveBackfillTicketAsync(initMmResults.BackfillTicketId);
				_playersMatchingIn = _backfillTicket.Properties.MatchProperties.Players.Select(p => p.Id).ToDictionary(k => k, _=> Time.realtimeSinceStartup);

				_connection = _backfillTicket.Connection;
				_region = initMmResults.MatchProperties.Region;
				_everyoneTeamName = initMmResults.MatchProperties.Teams.First().TeamName;
				_everyoneTeamID = initMmResults.MatchProperties.Teams.First().TeamId;

				BackfillEnabled = true;
			}
			else if(initMmResults != null)
			{
				// If backfilling isn't valid or turned on, just note which players are coming in via the initial matchmaker results
				_playersMatchingIn = initMmResults?.MatchProperties.Players.Select(p => p.Id).ToDictionary(k => k, _=> Time.realtimeSinceStartup);
			}

			Debug.Log("Players matching in from Unity matchmaking " + string.Join(',', _playersMatchingIn));
		}

		protected override async void OnDeinitialize()
		{
			await DeleteBackfillTicket();
			base.OnDeinitialize();
		}

		protected override async void OnTick()
		{
			base.OnTick();
			await UpdateBackfill();
		}

		// PRIVATE METHODS

		private async Task UpdateBackfill()
		{
			_backfillTimerMs += Time.deltaTime;
			if (_backfillTimerMs < _backfillIntervalMs) return;
			_backfillTimerMs = 0f;

			if (!BackfillEnabled)
			{
				if (_backfillTicket != null)
				{
					Debug.Log($"Backfill stopping, deleting ticket {_backfillTicket.Id}");
					await DeleteBackfillTicket();
				}

				return;
			}

			// Some of the players we were waiting for may have never connected, let's time them out
			var toRemove = _playersMatchingIn.Where(p => Time.realtimeSinceStartup - p.Value > PlayerJoiningExpiration).Select(p => p.Key);
			foreach (var key in toRemove)
			{
				_playersMatchingIn.Remove(key);
			}

			// Wait till we're in a NetworkedGame to start roster-based backfilling
			if(Context.NetworkGame == null || Context.NetworkGame.Object == null)
			{
				Debug.LogWarning("Context.NetworkGame is no initialized");
				return;
			}

			// Make a list of every player connected and currently expected to connect
			HashSet<string> allKnownPlayers = new HashSet<string>();
			foreach (var ngp in _networkPlayers)
			{
				if(string.IsNullOrEmpty(ngp.UnityID))
				{
					Debug.LogWarning("UnityID on Player was null or empty!");
					continue;
				}
				allKnownPlayers.Add(ngp.UnityID);
			}

			foreach (var unityId in _playersMatchingIn.Keys)
			{
				allKnownPlayers.Add(unityId);
			}

			// Check to see if the game is full and subsequently backfilling needs to stop
			int currentCount = allKnownPlayers.Count();
			int max = Global.MultiplayManager.MaxPlayers;
			if (currentCount >= max)
			{
				if (_backfillTicket != null)
				{
					Debug.Log($"Game full with {currentCount} current and expected players, removing backfill ticket {_backfillTicket.Id}");
					await DeleteBackfillTicket();
				}
				return;
			}

			// If the game is not full but we don't currently have a backfill ticket in progress, let's make one
			if (_backfillTicket == null)
			{
				await CreateBackfillTicket();
				return;
			}

			// If players have joined from outside the matchmaker or left the game, update the in-progress backfill ticket
			if (_pendingBackfillChange)
			{
				Debug.Log($"Roster changed, updating backfill {_backfillTicket.Id}");
				await UpdateBackfillTicket();
				_pendingBackfillChange = false;
				return;
			}

			// There's no pending roster changes on the server, so we can approve the existing backfilling ticket and bring more players into the match
			_backfillTicket = await MatchmakerService.Instance.ApproveBackfillTicketAsync(_backfillTicket.Id);
			int backfillPlayerCount = _backfillTicket.Properties.MatchProperties.Players.Count();
			if (backfillPlayerCount > currentCount)
			{
				// Matchmaking found new players that will now try to connect, note that we're expecting them
				Debug.Log($"New players expected from backfilling: {backfillPlayerCount - currentCount}. Was {currentCount}/{max}");
				foreach (var matchedPlayer in _backfillTicket.Properties.MatchProperties.Players)
				{
					if (!allKnownPlayers.Contains(matchedPlayer.Id))
					{
						// Add them to our set of expected players and mark the time
						_playersMatchingIn.Add(matchedPlayer.Id, Time.realtimeSinceStartup);
					}
				}
			}
		}

		/// <summary>
		/// Create a new backfill ticket from the state of the game and expected players
		/// A backfill ticket is automatically created by matchmaking if enabled on the pool rules. This method lets
		/// the game server start backfilling on demand (eg the game is no longer full, is in-between rounds, etc)
		/// </summary>
		private async Task CreateBackfillTicket()
		{
			var backfillPlayers = new List<Unity.Services.Matchmaker.Models.Player>();
			Team everyoneTeam = new Team(_everyoneTeamName, _everyoneTeamID, new List<string>());

			// Include current players
			foreach (var connectedPlayer in _networkPlayers)
			{
				backfillPlayers.Add(new Unity.Services.Matchmaker.Models.Player(connectedPlayer.UnityID));
				everyoneTeam.PlayerIds.Add(connectedPlayer.UnityID);
			}

			// Include players that we're still waiting to connect
			foreach (var expectedPlayer in _playersMatchingIn)
			{
				backfillPlayers.Add(new Unity.Services.Matchmaker.Models.Player(expectedPlayer.Key));
				everyoneTeam.PlayerIds.Add(expectedPlayer.Key);
			}

			// There's no backfill ticket, let's make one
			MatchProperties props = new MatchProperties(new List<Team>(){everyoneTeam}, backfillPlayers, _region);

			string backfillId = await MatchmakerService.Instance.CreateBackfillTicketAsync(
				new CreateBackfillTicketOptions(
					_queueName,
					_connection,
					attributes: null,
					new BackfillTicketProperties(props)));
			_backfillTicket = await MatchmakerService.Instance.ApproveBackfillTicketAsync(backfillId);
		}

		/// <summary>
		/// Rebuild the backfill ticket from the current state of the game and expected players
		/// </summary>
		private async Task UpdateBackfillTicket()
		{
			var backfillPlayers = new List<Unity.Services.Matchmaker.Models.Player>();

			// Include current players
			foreach (var connectedPlayer in _networkPlayers)
			{
				backfillPlayers.Add(new Unity.Services.Matchmaker.Models.Player(connectedPlayer.UnityID));
			}

			// Include players that we're still waiting to connect
			foreach (var expectedPlayer in _playersMatchingIn)
			{
				backfillPlayers.Add(new Unity.Services.Matchmaker.Models.Player(expectedPlayer.Key));
			}

			_backfillTicket.Properties.MatchProperties.Players.Clear();
			_backfillTicket.Properties.MatchProperties.Players.AddRange(backfillPlayers);

			await MatchmakerService.Instance.UpdateBackfillTicketAsync(_backfillTicket.Id, _backfillTicket);
		}

		/// <summary>
		/// Deletes a backfill ticket. If a backfill ticket is not deleted, it can optimistically collect
		/// "pending" matches and tie up players in matchmaking till the backfill ticket expires. Cleaning up
		/// backfill tickets is important to a short player matchmaking experience
		/// </summary>
		private async Task DeleteBackfillTicket()
		{
			if(_backfillTicket!=null)
			{
				await MatchmakerService.Instance.DeleteBackfillTicketAsync(_backfillTicket.Id);
				_backfillTicket = null;
			}
		}

		private async void OnApplicationQuit()
		{
			BackfillEnabled = false;
			await DeleteBackfillTicket();
		}
	}
}
#endif
