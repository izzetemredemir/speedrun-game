using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

namespace TPSBR
{
	/// <summary>
	/// Uses Unity matchmaker to connect to Multiplay game servers
	/// </summary>
	public sealed class Matchmaker : SceneService
	{
		// PUBLIC MEMBERS

		public Action<MultiplayAssignment> MatchFound;
		public Action<string>              MatchmakerFailed;

		// PRIVATE MEMBERS

		private float  _pollIntervalS = 1.0f;
		private float  _pollTimerS    = 0.0f;
		private string _ticketId      = string.Empty;

		// PUBLIC METHODS

		public async Task StartMatchmaker(string queueName)
		{
			// submit a ticket to the unity matchmaker
			CreateTicketResponse createTicketResponse;
			try
			{
				createTicketResponse = await MatchmakerService.Instance.CreateTicketAsync(
					new List<Unity.Services.Matchmaker.Models.Player>()
					{
						new Unity.Services.Matchmaker.Models.Player(Context.PlayerData.UnityID)
					}, new CreateTicketOptions(queueName));
			}
			catch (MatchmakerServiceException e)
			{
				MatchmakerFailed?.Invoke($"{e.Reason}: {e.Message}");
				return;
			}

			_ticketId = createTicketResponse.Id;
			Debug.Log("Ticket " + _ticketId);
			_pollTimerS = 0f;
		}

		public async Task CancelMatchmaker()
		{
			if (string.IsNullOrEmpty(_ticketId)) return;

			Debug.Log("Deleting ticket " + _ticketId);
			await MatchmakerService.Instance.DeleteTicketAsync(_ticketId);
			_ticketId = string.Empty;
			Debug.Log("Ticket deleted");
		}

		// SceneService ITNERFACE

		protected override async void OnDeinitialize()
		{
			await CancelMatchmaker();
			base.OnDeinitialize();
		}

		protected override async void OnTick()
		{
			base.OnTick();

			if (string.IsNullOrEmpty(_ticketId)) return;

			_pollTimerS += Time.deltaTime;
			if (_pollTimerS < _pollIntervalS) return;
			_pollTimerS = 0f;

			// poll for the results
			TicketStatusResponse ticketStatus = await MatchmakerService.Instance.GetTicketAsync(_ticketId);
			if (ticketStatus == null)
			{
				return;
			}

			MultiplayAssignment assignment = null;
			if (ticketStatus.Type == typeof(MultiplayAssignment))
			{
				assignment = ticketStatus.Value as MultiplayAssignment;
			}
			else
			{
				throw new NotImplementedException("Non-Multiplay assignment received. This sample only supports matchmaking into Multiplay-based games");
			}

			switch (assignment.Status)
			{
				case MultiplayAssignment.StatusOptions.InProgress:
					break;
				case MultiplayAssignment.StatusOptions.Found:
					Debug.Log("Match found: " + assignment.MatchId);
					_ticketId = string.Empty;
					MatchFound?.Invoke(assignment);
					break;
				case MultiplayAssignment.StatusOptions.Failed:
					_ticketId = string.Empty;
					MatchmakerFailed?.Invoke("Matchmaking failed. " + assignment.Message);
					break;
				case MultiplayAssignment.StatusOptions.Timeout:
					_ticketId = string.Empty;
					MatchmakerFailed?.Invoke("Matchmaking timed out");
					break;
				default:
					throw new InvalidOperationException();
			}
		}
	}
}
