using Fusion;
using Fusion.Plugin;

namespace TPSBR
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;

	using Random = UnityEngine.Random;

	public sealed class NetworkGame : ContextBehaviour, IPlayerJoined, IPlayerLeft
	{
		// PUBLIC MEMBERS

		public LevelGenerator LevelGenerator => _levelGenerator;

		[Networked, HideInInspector, Capacity(byte.MaxValue)]
		public NetworkArray<Player> Players => default;

		public int ActivePlayerCount { get; private set; }

		// PRIVATE MEMBERS

		[SerializeField]
		private Player _playerPrefab;
		[SerializeField]
		private GameplayMode[] _modePrefabs;

		[Header("Level Generation")]
		[SerializeField]
		private LevelGenerator _levelGenerator;
		[SerializeField]
		private int _fixedSeed = 0;
		[SerializeField]
		private int _levelSize = 30;
		[SerializeField]
		private int _areaCount = 5;

		[Space]
		[SerializeField]
		private ShrinkingArea _shrinkingArea;

		[Networked(OnChanged = nameof(OnSeedChanged), OnChangedTargets = OnChangedTargets.All)]
		private int _levelGeneratorSeed { get; set; }

		private PlayerRef                     _localPlayer;
		private Dictionary<PlayerRef, Player> _pendingPlayers      = new Dictionary<PlayerRef, Player>();
		private Dictionary<string, Player>    _disconnectedPlayers = new Dictionary<string, Player>();
		private FusionCallbacksHandler        _fusionCallbacks     = new FusionCallbacksHandler();
		private StatsRecorder                 _statsRecorder;
		private LogRecorder                   _logRecorder;
		private GameplayMode                  _gameplayMode;
		private bool                          _levelGenerated;
		private bool                          _isActive;

		// PUBLIC METHODS

		public void Initialize(EGameplayType gameplayType)
		{
			if (Object.HasStateAuthority == true)
			{
				var prefab = _modePrefabs.Find(t => t.Type == gameplayType);
				_gameplayMode = Runner.Spawn(prefab);
			}

			_localPlayer = Runner.LocalPlayer;

			_fusionCallbacks.DisconnectedFromServer -= OnDisconnectedFromServer;
			_fusionCallbacks.DisconnectedFromServer += OnDisconnectedFromServer;

			Runner.RemoveCallbacks(_fusionCallbacks);
			Runner.AddCallbacks(_fusionCallbacks);

			ActivePlayerCount = 0;
		}

		public void Activate()
		{
			_isActive = true;

			if (Object.HasStateAuthority == false)
			{
				GenerateLevel(_levelGeneratorSeed);

				if (ApplicationSettings.IsStrippedBatch == true)
				{
					Runner.GetComponent<NetworkPhysicsSimulation3D>().enabled = false;
					Runner.LagCompensation.enabled = false;
				}

				return;
			}

			if (_levelGenerator != null && _levelGenerator.enabled == true)
			{
				_levelGeneratorSeed = _fixedSeed == 0 ? Random.Range(999, 999999999) : _fixedSeed;
				GenerateLevel(_levelGeneratorSeed);
			}

			_gameplayMode.Activate();

			foreach (var playerRef in Runner.ActivePlayers)
			{
				SpawnPlayer(playerRef);
			}
		}

		public Player GetPlayer(PlayerRef playerRef)
		{
			if (playerRef.IsValid == false)
				return null;
			if (Object == null)
				return null;

			return Players[playerRef];
		}

		public int GetActivePlayerCount()
		{
			int players = 0;

			foreach (Player player in Players)
			{
				if (player == null)
					continue;

				var statistics = player.Statistics;
				if (statistics.IsValid == false)
					continue;

				if (statistics.IsEliminated == false)
				{
					players++;
				}
			}

			return players;
		}

		// NetworkBehaviour INTERFACE

		public override void FixedUpdateNetwork()
		{
			ActivePlayerCount = GetActivePlayerCount();

			if (Object.HasStateAuthority == false)
				return;

			if (_pendingPlayers.Count == 0)
				return;

			var playersToRemove = ListPool.Get<PlayerRef>(128);

			foreach (var playerPair in _pendingPlayers)
			{
				var playerRef = playerPair.Key;
				var player = playerPair.Value;

				if (player.IsInitialized == false)
					continue;

				playersToRemove.Add(playerRef);

				if (_disconnectedPlayers.TryGetValue(player.UserID, out Player disconnectedPlayer) == true)
				{
					// Remove original player, this is returning disconnected player
					Runner.Despawn(player.Object);

					_disconnectedPlayers.Remove(player.UserID);
					player = disconnectedPlayer;

					player.Object.AssignInputAuthority(playerRef);
				}

				Players.Set(playerRef, player);

#if UNITY_EDITOR
				player.gameObject.name = $"Player {player.Nickname}";
#endif

				_gameplayMode.PlayerJoined(player);
			}

			for (int i = 0; i < playersToRemove.Count; i++)
			{
				_pendingPlayers.Remove(playersToRemove[i]);
			}

			ListPool.Return(playersToRemove);
		}

		// IPlayerJoined/IPlayerLeft INTERFACES

		void IPlayerJoined.PlayerJoined(PlayerRef playerRef)
		{
			if (Runner.IsServer == false)
				return;
			if (_isActive == false)
				return;

			SpawnPlayer(playerRef);
		}

		void IPlayerLeft.PlayerLeft(PlayerRef playerRef)
		{
			if (playerRef.IsValid == false)
				return;
			if (Runner.IsServer == false)
				return;
			if (_isActive == false)
				return;

			Player player = Players[playerRef];

			Players.Set(playerRef, null);

			if (player != null)
			{
				if (player.UserID.HasValue() == true)
				{
					_disconnectedPlayers[player.UserID] = player;

					_gameplayMode.PlayerLeft(player);

					player.Object.RemoveInputAuthority();

#if UNITY_EDITOR
					player.gameObject.name = $"{player.gameObject.name} (Disconnected)";
#endif
				}
				else
				{
					_gameplayMode.PlayerLeft(player);

					// Player wasn't initilized properly, safe to despawn
					Runner.Despawn(player.Object);
				}
			}
		}

		// MonoBehaviour INTERFACE

		private void Update()
		{
			if (ApplicationSettings.RecordSession == false)
				return;
			if (Object == null)
				return;

			if (_statsRecorder == null)
			{
				string fileID = $"{System.DateTime.Now:yyyy-MM-dd-HH-mm-ss}";

				string statsFileName = $"FusionBR_{fileID}_Stats.log";
				string logFileName   = $"FusionBR_{fileID}_Log.log";

				_statsRecorder = new StatsRecorder();
				_statsRecorder.Initialize(ApplicationUtility.GetFilePath(statsFileName), fileID, "Time", "Players", "DeltaTime");

				_logRecorder = new LogRecorder();
				_logRecorder.Initialize(ApplicationUtility.GetFilePath(logFileName));
				_logRecorder.Write(fileID);

				Application.logMessageReceived -= OnLogMessage;
				Application.logMessageReceived += OnLogMessage;

				PrintInfo();
			}

			string time      = Time.realtimeSinceStartup.ToString(System.Globalization.CultureInfo.InvariantCulture);
			string players   = ActivePlayerCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
			string deltaTime = (Time.deltaTime * 1000.0f).ToString(System.Globalization.CultureInfo.InvariantCulture);

			_statsRecorder.Write(time, players, deltaTime);
		}

		private void OnLogMessage(string condition, string stackTrace, LogType type)
		{
			if (_logRecorder == null)
				return;

			_logRecorder.Write(condition);

			if (type == LogType.Exception)
			{
				_logRecorder.Write(stackTrace);
			}
		}

		private void OnDestroy()
		{
			if (_statsRecorder != null)
			{
				_statsRecorder.Deinitialize();
				_statsRecorder = null;
			}

			if (_logRecorder != null)
			{
				_logRecorder.Deinitialize();
				_logRecorder = null;
			}
		}

		// PRIVATE METHODS

		private void SpawnPlayer(PlayerRef playerRef)
		{
			if (Players[playerRef] != null || _pendingPlayers.ContainsKey(playerRef) == true)
			{
				Log.Error($"Player for {playerRef} is already spawned!");
				return;
			}

			var player = Runner.Spawn(_playerPrefab, inputAuthority: playerRef);

			Runner.SetPlayerAlwaysInterested(playerRef, player.Object, true);

			_pendingPlayers[playerRef] = player;

#if UNITY_EDITOR
			player.gameObject.name = $"Player Unknown (Pending)";
#endif
		}

		private void GenerateLevel(int seed)
		{
			if (_isActive == false || _levelGenerator == null || _levelGenerated == true || seed == 0)
				return;

			_levelGenerated = true;

			_levelGenerator.Generate(seed, _levelSize, _areaCount);

			Context.Map.OverrideParameters(_levelGenerator.Center, _levelGenerator.Dimensions);

			_shrinkingArea.OverrideParameters(_levelGenerator.Center, _levelGenerator.Dimensions.x * 0.5f, _levelGenerator.Dimensions.x * 0.5f, 50f);

			int areaOffset = Random.Range(0, _areaCount);
			for (int i = 0; i < _areaCount; i++)
			{
				int areaID = (i + areaOffset) % _areaCount;

				Vector2 shrinkEnd = _levelGenerator.Areas[areaID].Center * _levelGenerator.BlockSize;
				if (_shrinkingArea.SetEndCenter(shrinkEnd, i == _areaCount - 1) == true)
					break;
			}

			Debug.Log($"Level generated, center: {_levelGenerator.Center}, dimensions: {_levelGenerator.Dimensions}");

			if (Object.HasStateAuthority == false)
				return;

			Debug.Log($"Spawning {_levelGenerator.ObjectsToSpawn.Count} level generated objects");

			for (int i = 0; i < _levelGenerator.ObjectsToSpawn.Count; i++)
			{
				var spawnData = _levelGenerator.ObjectsToSpawn[i];
				var spawnedObject = Runner.Spawn(spawnData.Prefab, spawnData.Position, spawnData.Rotation);

				if (spawnData.IsConnector == true)
				{
					var connector = spawnedObject.GetComponent<IBlockConnector>();

					connector.SetMaterial(spawnData.AreaID, spawnData.Material);
					connector.SetHeight(spawnData.Height);
				}
			}
		}

		private void PrintInfo()
		{
			Debug.Log($"ApplicationUtility.DataPath: {ApplicationUtility.DataPath}");
			Debug.Log($"Environment.CommandLine: {Environment.CommandLine}");
			Debug.Log($"SystemInfo.deviceModel: {SystemInfo.deviceModel}");
			Debug.Log($"SystemInfo.deviceName: {SystemInfo.deviceName}");
			Debug.Log($"SystemInfo.deviceType: {SystemInfo.deviceType}");
			Debug.Log($"SystemInfo.processorCount: {SystemInfo.processorCount}");
			Debug.Log($"SystemInfo.processorFrequency: {SystemInfo.processorFrequency}");
			Debug.Log($"SystemInfo.processorType: {SystemInfo.processorType}");
			Debug.Log($"SystemInfo.systemMemorySize: {SystemInfo.systemMemorySize}");
		}

		// NETWORK CALLBACKS

		public static void OnSeedChanged(Changed<NetworkGame> changed)
		{
			changed.Behaviour.GenerateLevel(changed.Behaviour._levelGeneratorSeed);
		}

		private void OnDisconnectedFromServer(NetworkRunner runner)
		{
			if (runner != null)
			{
				runner.Simulation.SetLocalPlayer(_localPlayer);
			}
		}
	}
}
