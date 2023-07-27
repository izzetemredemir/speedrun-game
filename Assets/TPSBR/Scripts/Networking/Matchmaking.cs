using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion.Photon.Realtime;
using UnityEngine;

#pragma warning disable 4014

namespace TPSBR
{
	public class Matchmaking : SceneService, INetworkRunnerCallbacks
	{
		// PUBLIC MEMBERS

		public bool IsJoiningToLobby;
		public bool IsConnectedToLobby;

		public Action         LobbyJoined;
		public Action<string> LobbyJoinFailed;
		public Action         LobbyLeft;

		public event Action<NetworkRunner, PlayerRef>                                        PlayerJoined;
		public event Action<NetworkRunner, PlayerRef>                                        PlayerLeft;
		public event Action<NetworkRunner, Fusion.NetworkInput>                              Input;
		public event Action<NetworkRunner, PlayerRef, Fusion.NetworkInput>                   InputMissing;
		public event Action<NetworkRunner, ShutdownReason>                                   Shutdown;
		public event Action<NetworkRunner>                                                   ConnectedToServer;
		public event Action<NetworkRunner>                                                   DisconnectedFromServer;
		public event Action<NetworkRunner, NetworkRunnerCallbackArgs.ConnectRequest, byte[]> ConnectRequest;
		public event Action<NetworkRunner, NetAddress, NetConnectFailedReason>               ConnectFailed;
		public event Action<NetworkRunner, SimulationMessagePtr>                             UserSimulationMessage;
		public event Action<NetworkRunner, List<SessionInfo>>                                SessionListUpdated;
		public event Action<NetworkRunner, Dictionary<string, object>>                       CustomAuthenticationResponse;
		public event Action<NetworkRunner, HostMigrationToken>                               HostMigration;
		public event Action<NetworkRunner, PlayerRef, ArraySegment<byte>>                    ReliableDataReceived;
		public event Action<NetworkRunner>                                                   SceneLoadDone;
		public event Action<NetworkRunner>                                                   SceneLoadStart;

		// PRIVATE MEMBERS

		[SerializeField]
		private NetworkRunner _lobbyRunner;

		private string _lobbyName;
		private string _currentRegion;

		// PUBLIC METHODS

		public void CreateSession(SessionRequest request)
		{
			if (request.GameMode != GameMode.Server && request.GameMode != GameMode.Host)
				return;

			request.UserID      = Context.PlayerData.UserID;
			request.CustomLobby = _lobbyName;

			Global.Networking.StartGame(request);
		}

		public void JoinSession(SessionInfo session)
		{
			var request = new SessionRequest
			{
				UserID       = Context.PlayerData.UserID,
				GameMode     = GameMode.Client,
				GameplayType = session.GetGameplayType(),
				SessionName  = session.Name,
				ScenePath    = session.GetMapSetup().ScenePath,
				CustomLobby  = _lobbyName,
			};

			Global.Networking.StartGame(request);
		}

		public void JoinSession(string sessionName)
		{
			Global.Networking.StartGame(new SessionRequest()
			{
				UserID =  Context.PlayerData.UserID,
				GameMode = GameMode.Client,
				SessionName = sessionName,
				CustomLobby = _lobbyName
			});
		}

		public async Task JoinLobby(bool force = false)
		{
			if (IsJoiningToLobby == true)
				return;

			if (IsConnectedToLobby == true && force == false)
				return;

			IsJoiningToLobby = true;

			await LeaveLobby();

			_currentRegion = Context.RuntimeSettings.Region;
			PhotonAppSettings.Instance.AppSettings.FixedRegion = _currentRegion;

			var joinTask = _lobbyRunner.JoinSessionLobby(SessionLobby.Custom, _lobbyName);
			await joinTask;

			IsJoiningToLobby = false;
			IsConnectedToLobby = joinTask.Result.Ok;

			if (IsConnectedToLobby == true)
			{
				LobbyJoined?.Invoke();
			}
			else
			{
				LobbyJoinFailed?.Invoke(_currentRegion);
			}
		}

		public async Task LeaveLobby()
		{
			if (IsConnectedToLobby == true)
			{
				LobbyLeft?.Invoke();
			}

			IsConnectedToLobby = false;

			// HACK: Adding shutdown reason 'PhotonCloudTimeout' will prevent early return and cloud services
			// will be cleaned up (hulled) correctly. Without cleaned up services, rejoining lobby will always fail.
			// TODO: Can be removed after Fusion SDK fix
			await _lobbyRunner.Shutdown(false, ShutdownReason.PhotonCloudTimeout);
		}

		// SceneService INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_lobbyName = "FusionBR." + Application.version;

			_lobbyRunner.AddCallbacks(this);

			Context.Runner = _lobbyRunner;
		}

		protected override void OnDeinitialize()
		{
			if (_lobbyRunner != null)
			{
				_lobbyRunner.RemoveCallbacks(this);
			}

			base.OnDeinitialize();
		}

		protected override void OnActivate()
		{
			base.OnActivate();

			PhotonAppSettings.Instance.AppSettings.FixedRegion = Context.RuntimeSettings.Region;
		}

		protected override void OnTick()
		{
			base.OnTick();

			if (IsConnectedToLobby == true && _currentRegion != Global.RuntimeSettings.Region)
			{
				// Region changed, let's rejoin lobby
				JoinLobby(true);
			}
		}

		// INetworkRunnerCallbacks INTERFACE

		void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
		{
			PlayerJoined?.Invoke(runner, player);
		}

		void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
		{
			PlayerLeft?.Invoke(runner, player);
		}

		void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, Fusion.NetworkInput input)
		{
			Input?.Invoke(runner, input);
		}

		void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, Fusion.NetworkInput input)
		{
			InputMissing?.Invoke(runner, player, input);
		}

		void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
		{
			Shutdown?.Invoke(runner, shutdownReason);
		}

		void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
		{
			ConnectedToServer?.Invoke(runner);
		}

		void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner)
		{
			DisconnectedFromServer.Invoke(runner);
		}

		void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
		{
			ConnectRequest?.Invoke(runner, request, token);
		}

		void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
		{
			ConnectFailed?.Invoke(runner, remoteAddress, reason);
		}

		void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
		{
			UserSimulationMessage?.Invoke(runner, message);
		}

		void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
		{
			SessionListUpdated?.Invoke(runner, sessionList);
		}

		void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
		{
			CustomAuthenticationResponse?.Invoke(runner, data);
		}

		void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
		{
			HostMigration?.Invoke(runner, hostMigrationToken);
		}

		void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
		{
			ReliableDataReceived?.Invoke(runner, player, data);
		}

		void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
		{
			SceneLoadDone?.Invoke(runner);
		}

		void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner)
		{
			SceneLoadStart?.Invoke(runner);
		}
	}
}
