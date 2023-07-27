using Fusion;
using Fusion.Sockets;

namespace TPSBR
{
	using System;
	using System.Collections.Generic;

	public sealed class FusionCallbacksHandler : INetworkRunnerCallbacks
	{
		public Action<NetworkRunner, PlayerRef>                                        PlayerJoined;
		public Action<NetworkRunner, PlayerRef>                                        PlayerLeft;
		public Action<NetworkRunner, Fusion.NetworkInput>                              Input;
		public Action<NetworkRunner, PlayerRef, Fusion.NetworkInput>                   InputMissing;
		public Action<NetworkRunner, ShutdownReason>                                   Shutdown;
		public Action<NetworkRunner>                                                   ConnectedToServer;
		public Action<NetworkRunner>                                                   DisconnectedFromServer;
		public Action<NetworkRunner, NetworkRunnerCallbackArgs.ConnectRequest, byte[]> ConnectRequest;
		public Action<NetworkRunner, NetAddress, NetConnectFailedReason>               ConnectFailed;
		public Action<NetworkRunner, SimulationMessagePtr>                             UserSimulationMessage;
		public Action<NetworkRunner, List<SessionInfo>>                                SessionListUpdated;
		public Action<NetworkRunner, Dictionary<string, object>>                       CustomAuthenticationResponse;
		public Action<NetworkRunner, HostMigrationToken>                               HostMigration;
		public Action<NetworkRunner, PlayerRef, ArraySegment<byte>>                    ReliableDataReceived;
		public Action<NetworkRunner>                                                   SceneLoadDone;
		public Action<NetworkRunner>                                                   SceneLoadStart;

		void INetworkRunnerCallbacks.OnPlayerJoined                (NetworkRunner runner, PlayerRef player)                                               { if (PlayerJoined                 != null) { PlayerJoined                (runner, player);                } }
		void INetworkRunnerCallbacks.OnPlayerLeft                  (NetworkRunner runner, PlayerRef player)                                               { if (PlayerLeft                   != null) { PlayerLeft                  (runner, player);                } }
		void INetworkRunnerCallbacks.OnInput                       (NetworkRunner runner, Fusion.NetworkInput input)                                      { if (Input                        != null) { Input                       (runner, input);                 } }
		void INetworkRunnerCallbacks.OnInputMissing                (NetworkRunner runner, PlayerRef player, Fusion.NetworkInput input)                    { if (InputMissing                 != null) { InputMissing                (runner, player, input);         } }
		void INetworkRunnerCallbacks.OnShutdown                    (NetworkRunner runner, ShutdownReason shutdownReason)                                  { if (Shutdown                     != null) { Shutdown                    (runner, shutdownReason);        } }
		void INetworkRunnerCallbacks.OnConnectedToServer           (NetworkRunner runner)                                                                 { if (ConnectedToServer            != null) { ConnectedToServer           (runner);                        } }
		void INetworkRunnerCallbacks.OnDisconnectedFromServer      (NetworkRunner runner)                                                                 { if (DisconnectedFromServer       != null) { DisconnectedFromServer      (runner);                        } }
		void INetworkRunnerCallbacks.OnConnectRequest              (NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { if (ConnectRequest               != null) { ConnectRequest              (runner, request, token);        } }
		void INetworkRunnerCallbacks.OnConnectFailed               (NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)        { if (ConnectFailed                != null) { ConnectFailed               (runner, remoteAddress, reason); } }
		void INetworkRunnerCallbacks.OnUserSimulationMessage       (NetworkRunner runner, SimulationMessagePtr message)                                   { if (UserSimulationMessage        != null) { UserSimulationMessage       (runner, message);               } }
		void INetworkRunnerCallbacks.OnSessionListUpdated          (NetworkRunner runner, List<SessionInfo> sessionList)                                  { if (SessionListUpdated           != null) { SessionListUpdated          (runner, sessionList);           } }
	    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)                                { if (CustomAuthenticationResponse != null) { CustomAuthenticationResponse(runner, data);                  } }
		void INetworkRunnerCallbacks.OnHostMigration               (NetworkRunner runner, HostMigrationToken hostMigrationToken)                          { if (HostMigration                != null) { HostMigration               (runner, hostMigrationToken);    } }
	    void INetworkRunnerCallbacks.OnReliableDataReceived        (NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)                      { if (ReliableDataReceived         != null) { ReliableDataReceived        (runner, player, data);          } }
	    void INetworkRunnerCallbacks.OnSceneLoadDone               (NetworkRunner runner)                                                                 { if (SceneLoadDone                != null) { SceneLoadDone               (runner);                        } }
	    void INetworkRunnerCallbacks.OnSceneLoadStart              (NetworkRunner runner)                                                                 { if (SceneLoadStart               != null) { SceneLoadStart              (runner);                        } }
	}
}
