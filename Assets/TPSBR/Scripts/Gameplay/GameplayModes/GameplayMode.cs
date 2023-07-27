using Fusion;
using System;

namespace TPSBR
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	public struct KillData : INetworkStruct
	{
		public PlayerRef KillerRef;
		public PlayerRef VictimRef;
		public EHitType  HitType;
		public bool      Headshot { get { return _flags.IsBitSet(0); } set { _flags.SetBit(0, value); } }

		private byte     _flags;
	}

	public struct RespawnRequest
	{
		public PlayerRef PlayerRef;
		public TickTimer Timer;
	}

	public enum EGameplayType
	{
		None,
		Deathmatch,
		Elimination,
		BattleRoyale,
	}

	public abstract class GameplayMode : ContextBehaviour
	{
		public enum EState
		{
			None,
			Active,
			Finished,
		}

		public const byte MAX_PLAYERS = 200;

		public string GameplayName;

		public int    MaxPlayers;
		public short  ScorePerKill;
		public short  ScorePerDeath;
		public short  ScorePerSuicide;
		public float  RespawnTime;
		public float  TimeLimit;
		public float  BackfillTimeLimit;

		public Announcement[] Announcements;

		// PUBLIC MEMBERS

		public EGameplayType         Type          => _type;
		public float                 Time          => (Runner.Simulation.Tick - _startTick) * Runner.DeltaTime;
		public float                 RemainingTime => _endTimer.IsRunning == true ? _endTimer.RemainingTime(Runner).Value : 0f;

		[Networked, HideInInspector]
		public EState                State         { get; private set; }

		public List<SpawnPoint>      SpawnPoints   => _allSpawnPoints;
		public ShrinkingArea         ShrinkingArea => _shrinkingArea;

		public Action<PlayerRef>     OnPlayerJoinedGame;
		public Action<string>        OnPlayerLeftGame;
		public Action<KillData>      OnAgentDeath;
		public Action<PlayerRef>     OnPlayerEliminated;

		// PROTECTED MEMBERS

		[Networked, HideInInspector]
		protected int _startTick { get; set; }
		[Networked, HideInInspector]
		protected TickTimer _endTimer { get; private set; }
		[Networked, HideInInspector]
		protected ShrinkingArea _shrinkingArea { get; set; }

		// PRIVATE MEMBERS

		[SerializeField]
		private EGameplayType _type;
		[SerializeField]
		private bool _useShrinkingArea;
		[SerializeField]
		private float _maxKillInRowDelay = 3.5f;

		private Queue<RespawnRequest>    _respawnRequests       = new Queue<RespawnRequest>(16);

		private List<SpawnPoint>         _allSpawnPoints        = new List<SpawnPoint>();
		private List<SpawnPoint>         _availableSpawnPoints  = new List<SpawnPoint>();
		private DefaultPlayerComparer    _playerComparer        = new DefaultPlayerComparer();
		private float                    _backfillTimerS;

		// PUBLIC METHODS

		public void Activate()
		{
			if (Runner.IsServer == false)
				return;
			if (State != EState.None)
				return;

			_startTick = Runner.Simulation.Tick;

			if (TimeLimit > 0f)
			{
				_endTimer = TickTimer.CreateFromSeconds(Runner, TimeLimit);
			}

			if (_useShrinkingArea == true && Object.HasStateAuthority == true)
			{
				_shrinkingArea = Runner.SimulationUnityScene.GetComponent<ShrinkingArea>();
				if (_shrinkingArea != null && Object.HasStateAuthority == true)
				{
					_shrinkingArea.Activate();

					_shrinkingArea.ShrinkingAnnounced += OnShrinkingAreaAnnounced;
				}
			}

			Runner.SimulationUnityScene.GetComponents(_allSpawnPoints);

			for (int i = 0; i < _allSpawnPoints.Count; i++)
			{
				if (_allSpawnPoints[i].SpawnEnabled == true)
				{
					_availableSpawnPoints.Add(_allSpawnPoints[i]);
				}
			}

			if (_shrinkingArea != null && Object.HasStateAuthority == true)
			{
				OnShrinkingAreaAnnounced(_shrinkingArea.Center, _shrinkingArea.Radius);
			}

			State = EState.Active;

			OnActivate();
		}

		public void AgentDeath(Agent victim, HitData hitData)
		{
			if (Runner.IsServer == false)
				return;
			if (State != EState.Active)
				return;

			var victimRef        = victim.Object.InputAuthority;
			var victimPlayer     = Context.NetworkGame.GetPlayer(victimRef);
			var victimStatistics = victimPlayer != null ? victimPlayer.Statistics : default;

			var respawnTime = GetRespawnTime(victimStatistics);
			if (respawnTime >= 0f)
			{
				var respawnTimer = TickTimer.CreateFromSeconds(Runner, respawnTime);
				victimStatistics.RespawnTimer = respawnTimer;
				_respawnRequests.Enqueue(new RespawnRequest()
				{
					PlayerRef = victimRef,
					Timer     = respawnTimer,
				});
			}
			else
			{
				victimStatistics.IsEliminated = true;
			}

			victimStatistics.IsAlive           = false;
			victimStatistics.Deaths           += 1;
			victimStatistics.Score            += ScorePerDeath;
			victimStatistics.KillsWithoutDeath = 0;

			var killerRef         = hitData.InstigatorRef;
			var killerPlayer      = killerRef.IsValid == true ? Context.NetworkGame.GetPlayer(killerRef) : default;
			var killerStatistics  = killerPlayer != null ? killerPlayer.Statistics : default;

			if (killerRef == victimRef)
			{
				victimStatistics.Score += ScorePerSuicide;
			}
			else
			{
				killerStatistics.Kills += 1;
				killerStatistics.Score += ScorePerKill;
				killerStatistics.KillsWithoutDeath += 1;

				if (killerStatistics.KillsInRowCooldown.Expired(Runner) == false)
				{
					killerStatistics.KillsInRow += 1;
				}
				else
				{
					killerStatistics.KillsInRow = 1;
				}

				killerStatistics.KillsInRowCooldown = TickTimer.CreateFromSeconds(Runner, _maxKillInRowDelay);
			}

			AgentDeath(ref victimStatistics, ref killerStatistics);

			if (victimPlayer != null)
			{
				victimPlayer.UpdateStatistics(victimStatistics);
			}

			if (killerPlayer != null && killerPlayer != victimPlayer)
			{
				killerPlayer.UpdateStatistics(killerStatistics);
			}

			RecalculatePositions();

			var killData = new KillData()
			{
				KillerRef = killerStatistics.PlayerRef,
				VictimRef = victimRef,
				Headshot  = hitData.IsCritical,
				HitType   = hitData.HitType,
			};

			RPC_AgentDeath(killData);

			if (victimStatistics.IsEliminated == true)
			{
				RPC_PlayerEliminated(victimRef);
			}

			CheckWinCondition();
		}

		public Transform GetRandomSpawnPoint(float minDistanceFromAgents)
		{
			if (_availableSpawnPoints.SafeCount() == 0)
				return null;

			while (minDistanceFromAgents > 1.0f)
			{
				float minSqrDistanceFromAgents = minDistanceFromAgents * minDistanceFromAgents;

				for (int i = 0, count = Mathf.Min(5 + _availableSpawnPoints.Count, 25); i < count; ++i)
				{
					Transform spawnPoint = _availableSpawnPoints.GetRandom().transform;
					bool      isValid    = true;

					foreach (var player in Context.NetworkGame.Players)
					{
						if (player == null)
							continue;

						var agent = player.ActiveAgent;
						if (agent == null)
							continue;

						if (Vector3.SqrMagnitude(agent.transform.position - spawnPoint.position) < minSqrDistanceFromAgents)
						{
							isValid = false;
							break;
						}
					}

					if (isValid == true)
						return spawnPoint;
				}

				minDistanceFromAgents *= 0.5f;
			}

			return _availableSpawnPoints.GetRandom().transform;
		}

		public void ChangeSpectatorTarget(bool next)
		{
			var observedPlayerRef = Context.ObservedPlayerRef;

			while (true)
			{
				if (observedPlayerRef > MAX_PLAYERS)
				{
					observedPlayerRef = PlayerRef.None;
				}
				else if (observedPlayerRef < PlayerRef.None)
				{
					observedPlayerRef = MAX_PLAYERS;
				}

				observedPlayerRef += next == true ? 1 : -1;

				Player observedPlayer = Context.NetworkGame.GetPlayer(observedPlayerRef);
				if (observedPlayer == null)
					continue;

				if (observedPlayer.Statistics.IsEliminated == true)
					continue;

				break;
			}

			var localPlayer = Context.NetworkGame.GetPlayer(Context.LocalPlayerRef);
			localPlayer.SetObservedPlayer(observedPlayerRef);
		}

		public void PlayerJoined(Player player)
		{
			var statistics = player.Statistics;

			statistics.PlayerRef = player.Object.InputAuthority;

			PreparePlayerStatistics(ref statistics);
			player.UpdateStatistics(statistics);

			if (statistics.IsEliminated == false)
			{
				TrySpawnAgent(player);
			}

			RecalculatePositions();

			Context.Backfill.PlayerJoined(player);

			RPC_PlayerJoinedGame(player.Object.InputAuthority);
		}

		public void PlayerLeft(Player player)
		{
			if (Runner.IsServer == false)
				return;
			if (State == EState.Finished)
				return;

			player.DespawnAgent();

			RecalculatePositions();

			Context.Backfill.PlayerLeft(player);

			RPC_PlayerLeftGame(player.Object.InputAuthority, player.Nickname);

			CheckWinCondition();
		}

		public void StopGame()
		{
			if (Object == null || Object.HasStateAuthority == false)
			{
				Global.Networking.StopGame();
				return;
			}

			StartCoroutine(StopGameCoroutine());
		}

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			Context.GameplayMode = this;
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			if (_shrinkingArea != null)
			{
				_shrinkingArea.ShrinkingAnnounced -= OnShrinkingAreaAnnounced;
			}
		}

		public override void FixedUpdateNetwork()
		{
			if (Object.HasStateAuthority == false)
				return;

			switch (State)
			{
				case EState.Active:   FixedUpdateNetwork_Active();   break;
				case EState.Finished: FixedUpdateNetwork_Finished(); break;
			}
		}

		// GameplayMode INTERFACE

		protected virtual void OnActivate() { }

		protected virtual void TrySpawnAgent(Player player)
		{
			Transform spawnPoint = GetRandomSpawnPoint(100.0f);

			var spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
			var spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

			SpawnAgent(player.Object.InputAuthority, spawnPosition, spawnRotation);
		}

		protected virtual void AgentDeath(ref PlayerStatistics victimStatistics, ref PlayerStatistics killerStatistics)
		{
		}

		protected virtual void PreparePlayerStatistics(ref PlayerStatistics playerStatistics)
		{
		}

		protected virtual void SortPlayers(List<PlayerStatistics> allStatistics)
		{
			allStatistics.Sort(_playerComparer);
		}

		protected virtual float GetRespawnTime(PlayerStatistics playerStatistics)
		{
			return RespawnTime;
		}

		protected abstract void CheckWinCondition();

		// PROTECTED METHODS

		protected Agent SpawnAgent(PlayerRef playerRef, Vector3 position, Quaternion rotation)
		{
			var player = Context.NetworkGame.GetPlayer(playerRef);
			if (player.AgentPrefabID.IsValid == false)
			{
				throw new InvalidOperationException(nameof(player.AgentPrefabID));
			}

			var agentObject = Runner.Spawn(player.AgentPrefabID, position, rotation, playerRef);
			var agent       = agentObject.GetComponent<Agent>();

			Runner.SetPlayerAlwaysInterested(playerRef, agentObject, true);

			var statistics          = player.Statistics;
			statistics.IsAlive      = true;
			statistics.RespawnTimer = default;

			player.UpdateStatistics(statistics);
			player.SetActiveAgent(agent);

			return agent;
		}

		protected void FinishGameplay()
		{
			if (State != EState.Active)
				return;
			if (Runner.IsServer == false)
				return;

			State = EState.Finished;
			Runner.SessionInfo.IsOpen = false;
			Context.Backfill.BackfillEnabled = false;

			if (Application.isBatchMode == true)
			{
				StartCoroutine(ShutdownCoroutine());
			}
		}

		protected void SetSpectatorTargetToBestPlayer(Player spectatorPlayer)
		{
			var bestPlayer = PlayerRef.None;
			int bestPosition = int.MaxValue;

			foreach (var player in Context.NetworkGame.Players)
			{
				if (player == null)
					continue;

				var statistics = player.Statistics;
				if (statistics.IsEliminated == true)
					continue;

				int position = statistics.Position > 0 ? statistics.Position : 1000;

				if (position < bestPosition)
				{
					bestPlayer = statistics.PlayerRef;
					bestPosition = position;
				}
			}

			spectatorPlayer.SetObservedPlayer(bestPlayer);
		}

		// PRIVATE METHODS

		private void FixedUpdateNetwork_Active()
		{
			while (_respawnRequests.Count > 0)
			{
				var respawnRequest = _respawnRequests.Peek();
				if (respawnRequest.Timer.Expired(Runner) == false)
					break;

				_respawnRequests.Dequeue();
				Respawn(respawnRequest.PlayerRef);
			}

			_backfillTimerS += UnityEngine.Time.deltaTime;
			if (_backfillTimerS > BackfillTimeLimit)
			{
				Context.Backfill.BackfillEnabled = false;
			}

			if (_endTimer.Expired(Runner) == true)
			{
				FinishGameplay();
			}
		}

		private void FixedUpdateNetwork_Finished()
		{
		}

		private void Respawn(PlayerRef playerRef)
		{
			var player = Context.NetworkGame.GetPlayer(playerRef);
			if (player == null)
				return; // Player is not present anymore

			player.DespawnAgent();

			TrySpawnAgent(player);
		}

		private void RecalculatePositions()
		{
			var allStatistics = ListPool.Get<PlayerStatistics>(MAX_PLAYERS);

			foreach (var player in Context.NetworkGame.Players)
			{
				if (player == null)
					continue;

				var statistics = player.Statistics;
				if (statistics.IsValid == false)
					continue;

				allStatistics.Add(statistics);
			}

			SortPlayers(allStatistics);

			for (int i = 0; i < allStatistics.Count; i++)
			{
				var statistics = allStatistics[i];

				statistics.Position = (byte)(i + 1);
				Context.NetworkGame.GetPlayer(statistics.PlayerRef).UpdateStatistics(statistics);
			}

			ListPool.Return(allStatistics);
		}

		private void OnShrinkingAreaAnnounced(Vector3 center, float radius)
		{
			_availableSpawnPoints.Clear();

			var radiusSqr = radius * radius;

			foreach (var spawnPoint in _allSpawnPoints)
			{
				if (spawnPoint.SpawnEnabled == false)
					continue;

				var direction = spawnPoint.transform.position - center;
				direction.y   = 0f;

				if (direction.sqrMagnitude <= radiusSqr)
				{
					_availableSpawnPoints.Add(spawnPoint);
				}
			}
		}

		private IEnumerator StopGameCoroutine()
		{
			RPC_StopGame();

			Context.Backfill.BackfillEnabled = false;

			yield return new WaitForSecondsRealtime(0.25f);

			Global.Networking.StopGame();
		}

		private IEnumerator ShutdownCoroutine()
		{
			yield return new WaitForSecondsRealtime(20.0f);

			Debug.LogWarning("Shutting down...");
			Application.Quit();
		}

		// RPCs

		[Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
		private void RPC_AgentDeath(KillData killData)
		{
			OnAgentDeath?.Invoke(killData);
		}

		[Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
		private void RPC_PlayerEliminated(PlayerRef playerRef)
		{
			OnPlayerEliminated?.Invoke(playerRef);
		}

		[Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
		private void RPC_PlayerJoinedGame(PlayerRef playerRef)
		{
			OnPlayerJoinedGame?.Invoke(playerRef);
		}

		[Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
		private void RPC_PlayerLeftGame(PlayerRef playerRef, string nickname)
		{
			OnPlayerLeftGame?.Invoke(nickname);
		}

		[Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
		private void RPC_StopGame()
		{
			Global.Networking.StopGame(Networking.STATUS_SERVER_CLOSED);
		}

		// HELPERS

		private class DefaultPlayerComparer : IComparer<PlayerStatistics>
		{
			public int Compare(PlayerStatistics x, PlayerStatistics y)
			{
				return y.Score.CompareTo(x.Score);
			}
		}
	}
}
