#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
using System;
using UnityEngine;

namespace TPSBR
{
	/// <summary>
	/// MultiplayManager works only on server
	/// </summary>
	public sealed class MultiplayManager : MonoBehaviour
	{
		public async void StartMultiplay(SessionRequest sessionRequest, StandaloneConfiguration configuration)
		{
			throw new NotSupportedException();
		}
	}
}
#else
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using UnityEngine;

namespace TPSBR
{
	using System;

	public sealed class MultiplayManager : MonoBehaviour
	{
		// PUBLIC MEMBERS

		public bool QueryProtocol = false;
		public bool Matchmaking = false;
		public bool Backfill = false;
		public int MaxPlayers;
		public MatchmakingResults MatchmakingResults;

		// PRIVATE MEMBERS

		private MultiplayEventCallbacks _multiplayEventCallbacks;
		private IServerEvents _serverEvents;
		private IServerQueryHandler _serverQueryHandler;
		private SessionRequest _sessionRequest;

		private bool _sqpInitialized = false;

		// PUBLIC METHODS

		public async void StartMultiplay(SessionRequest sessionRequest, StandaloneConfiguration configuration)
		{
			_sessionRequest = sessionRequest;
			QueryProtocol = configuration.QueryProtocol;
			Matchmaking = configuration.Matchmaking;
			Backfill = configuration.Backfill;
			MaxPlayers = configuration.MaxPlayers;

			LogServerConfig();

			// Setup allocations
			_multiplayEventCallbacks = new MultiplayEventCallbacks();
			_multiplayEventCallbacks.Allocate += OnAllocate;
			_multiplayEventCallbacks.Deallocate += OnDeallocate;
			_multiplayEventCallbacks.Error += OnError;
			_serverEvents = await MultiplayService.Instance.SubscribeToServerEventsAsync(_multiplayEventCallbacks);

			// It's possible this is a cold start allocation (no event will fire)
			if (!string.IsNullOrEmpty(MultiplayService.Instance.ServerConfig.AllocationId))
			{
				await StartGame();
			}
		}

		// MonoBehaviour INTERFACE

		private async void Awake()
		{
			if (UnityServices.State == ServicesInitializationState.Uninitialized)
			{
				await UnityServices.InitializeAsync();
			}
		}

		private void Update()
		{
			if (QueryProtocol)
			{
				UpdateSqp();
			}
		}

		// PRIVATE METHODS

		private void UpdateSqp()
		{
			if (!_sqpInitialized) InitializeSqp();
			_serverQueryHandler.CurrentPlayers = 1;// (ushort)Global.Networking.PeerCount;
			_serverQueryHandler.UpdateServerCheck();
		}

		private async void InitializeSqp()
		{
			_serverQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(
				(ushort)_sessionRequest.MaxPlayers,
				_sessionRequest.DisplayName,
				_sessionRequest.GameplayType.ToString().ToLowerInvariant(),
				Application.version,
				"mapname");
			_sqpInitialized = true;
		}

		private void OnError(MultiplayError error)
		{
			LogServerConfig();
			throw new NotImplementedException();
		}

		private void OnDeallocate(MultiplayDeallocation deallocation)
		{
			Debug.Log("Deallocated");
			LogServerConfig();

			MatchmakingResults = null;

			// Hack for now, just exit the application on deallocate
			Application.Quit();
		}

		private async void OnAllocate(MultiplayAllocation allocation)
		{
			Debug.Log("Allocated");
			LogServerConfig();

			await StartGame();
		}

		private async Task StartGame()
		{
			if (!Matchmaking)
			{
				Debug.Log("Matchmaking not enabled, just starting a game");
				Global.Networking.StartGame(_sessionRequest);
				return;
			}

			MatchmakingResults = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();
			Debug.Log($"Game produced by matchmaker generator {MatchmakingResults.GeneratorName}, Queue {MatchmakingResults.QueueName}, Pool {MatchmakingResults.PoolName}, BackfillTicketId {MatchmakingResults.BackfillTicketId}");

			_sessionRequest.SessionName = "mm-" + MatchmakingResults.MatchId;
			Global.Networking.StartGame(_sessionRequest);

			while(!Global.Networking.IsConnected)
			{
				await Task.Delay(250);
			}

			if(QueryProtocol)
			{
				Debug.Log("IMultiplayService.ReadyServerForPlayersAsync()");
				await MultiplayService.Instance.ReadyServerForPlayersAsync();
			}
		}

		private void LogServerConfig()
		{
			var serverConfig = MultiplayService.Instance.ServerConfig;
			Debug.Log($"Server ID[{serverConfig.ServerId}], AllocationId[{serverConfig.AllocationId}], Port[{serverConfig.Port}], QueryPort[{serverConfig.QueryPort}], LogDirectory[{serverConfig.ServerLogDirectory}]");
		}
	}
}
#endif
