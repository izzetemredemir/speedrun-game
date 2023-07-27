using Fusion;
using UnityEngine;

namespace TPSBR
{
	public class ShrinkingArea : ContextBehaviour
	{
		// PUBLIC MEMBERS

		public bool      IsActive       { get { return _flags.IsBitSet(0); } private set { _flags = _flags.SetBitNoRef(0, value); } }
		public bool      IsShrinking    { get { return _flags.IsBitSet(1); } private set { _flags = _flags.SetBitNoRef(1, value); } }
		public bool      IsAnnounced    { get { return _flags.IsBitSet(2); } private set { _flags = _flags.SetBitNoRef(2, value); } }
		public bool      IsPaused       { get { return _flags.IsBitSet(3); } private set { _flags = _flags.SetBitNoRef(3, value); } }

		[Networked, HideInInspector]
		public float     ShrinkDelay    { get; private set; }

		public TickTimer NextShrinking  => _nextShrinking;
		public bool      IsFinished     => Radius <= _endRadius;

		[Networked, HideInInspector]
		public  Vector3  Center        { get; set; }
		[Networked, HideInInspector]
		public  float    Radius        { get; set; }
		[Networked, HideInInspector]
		public  Vector3  ShrinkCenter  { get; set; }
		[Networked, HideInInspector]
		public  float    ShrinkRadius  { get; set; }

		public System.Action<Vector3, float> ShrinkingAnnounced;

		// PRIVATE MEMBERS

		[Header("Size")]
		[SerializeField]
		private float     _mapRadius = 200f;
		[SerializeField]
		private float     _startRadius = 100f;
		[SerializeField]
		private float     _endRadius = 40f;
		[SerializeField]
		private Transform _shrinkArea;
		[SerializeField]
		private Transform _shrinkAreaTarget;

		[Header("Timing")]
		[SerializeField]
		private float _shrinkStartDelay = 30f;
		[SerializeField]
		private float _minShrinkDelay = 35f;
		[SerializeField]
		private float _maxShrinkDelay = 90f;
		[SerializeField]
		private int   _minShrinkDelayPlayers = 2;
		[SerializeField]
		private int   _maxShrinkDelayPlayers = 60;
		[SerializeField]
		private float _shrinkDuration = 20f;
		[SerializeField]
		private float _shrinkAnnounceDuration = 30f;
		[SerializeField]
		private int   _shrinkSteps = 5;

		[Header("Damage")]
		[SerializeField]
		private float _damagePerTick = 5f;
		[SerializeField]
		private float _damageTickTime = 1f;

		[Networked]
		private byte      _flags         { get; set; }
		[Networked]
		private TickTimer _nextShrinking { get; set; }

		private int         _currentStage;
		private AreaStage[] _calculatedStages;

		private int       _shrinkStartTick;
		private int       _shrinkEndTick;

		private Material  _shrinkAreaMaterial;
		private Material  _shrinkAreaTargetMaterial;
		private int       _materialRadiusID;

		private TickTimer _damageTimer;

		// PUBLIC METHODS

		public void Activate()
		{
			if (IsActive == true)
				return;
			if (Object.HasStateAuthority == false)
				return;

			IsActive = true;

			if (_calculatedStages == null)
			{
				var endCenter = new Vector2(transform.position.x, transform.position.z) + Random.insideUnitCircle * (_mapRadius - _endRadius);
				CalculateStages(endCenter);
			}

			var startStage = _calculatedStages[0];

			Center = new Vector3(startStage.Center.x, transform.position.y, startStage.Center.y);
			Radius = startStage.Radius;

			_damageTimer = TickTimer.CreateFromSeconds(Runner, _damageTickTime);
			SetNextShrinkingTimer(_shrinkStartDelay);
		}

		public void Deactivate()
		{
			if (IsActive == false)
				return;
			if (Object.HasStateAuthority == false)
				return;

			IsActive = false;
		}

		public void Pause(bool pause)
		{
			if (IsPaused == pause)
				return;

			IsPaused = pause;

			if (pause == false)
			{
				_damageTimer = TickTimer.CreateFromSeconds(Runner, _damageTickTime);
				SetNextShrinkingTimer();
			}
		}

		public void OverrideParameters(Vector3 center, float mapRadius, float startRadius, float endRadius)
		{
			transform.position = center;

			_mapRadius = mapRadius;
			_startRadius = startRadius;
			_endRadius = endRadius;
		}

		public bool SetEndCenter(Vector2 endCenter, bool force = false)
		{
			if (Object.HasStateAuthority == false)
				return false;

			var position = new Vector2(transform.position.x, transform.position.z);

			if (Vector2.Distance(endCenter, position) > _mapRadius - _endRadius)
			{
				if (force == false)
					return false;

				var direction = endCenter - position;
				endCenter = position + direction.normalized * Random.value * (_mapRadius - _endRadius);
			}

			CalculateStages(endCenter);
			return true;
		}

		// NetworkBehaviour INTERFACE

		public override void FixedUpdateNetwork()
		{
			if (IsActive == false || IsPaused == true)
				return;

			if (Object.HasStateAuthority == false)
				return;

			if (IsPaused == false)
			{
				if (_nextShrinking.IsRunning == true)
				{
					var remainingTime = _nextShrinking.RemainingTime(Runner);

					if (remainingTime <= 0f)
					{
						Shrink();
					}
					else if (remainingTime <= _shrinkAnnounceDuration)
					{
						Announce();
					}
				}

				if (IsShrinking == true)
				{
					UpdateShrinking();
				}
			}

			if (_damageTimer.IsRunning == true && _damageTimer.Expired(Runner) == true)
			{
				UpdateDamage();
			}
		}

		public override void Render()
		{
			if (IsActive == true)
			{
				_shrinkArea.SetActive(true);

				_shrinkArea.position   = Center;
				_shrinkArea.localScale = new Vector3(Radius * 2f, 1f, Radius * 2f);
				_shrinkAreaMaterial.SetFloat(_materialRadiusID, Radius);

				if (IsAnnounced == true)
				{
					_shrinkAreaTarget.position   = ShrinkCenter;
					_shrinkAreaTarget.localScale = new Vector3(ShrinkRadius * 2f, 1f, ShrinkRadius * 2f);
					_shrinkAreaTargetMaterial.SetFloat(_materialRadiusID, ShrinkRadius);

					_shrinkAreaTarget.SetActive(true);
				}
				else
				{
					_shrinkAreaTarget.SetActive(false);
				}
			}
			else
			{
				_shrinkAreaTarget.SetActive(false);
				_shrinkArea.SetActive(false);
			}
		}

		// MONOBEHAVIOUR

		protected void Start()
		{
			_shrinkAreaMaterial = _shrinkArea.GetComponentInChildren<MeshRenderer>(true).material;
			_shrinkAreaTargetMaterial = _shrinkAreaTarget.GetComponentInChildren<MeshRenderer>(true).material;
			_materialRadiusID = Shader.PropertyToID("Radius");
		}

		protected void OnDrawGizmosSelected()
		{
			var tmpColor = Gizmos.color;

			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(transform.position, _startRadius);

			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(transform.position, _endRadius);

			Gizmos.color = tmpColor;
		}

		// PRIVATE METHODS

		private void Announce()
		{
			if (IsActive == false || IsAnnounced == true)
				return;
			if (Object.HasStateAuthority == false)
				return;
			if (IsFinished == true)
				return;

			IsAnnounced = true;

			_currentStage += 1;
			var stage = _calculatedStages[_currentStage];

			ShrinkRadius = stage.Radius;
			ShrinkCenter = new Vector3(stage.Center.x, transform.position.y, stage.Center.y);

			ShrinkingAnnounced?.Invoke(ShrinkCenter, ShrinkRadius);
		}

		private void Shrink()
		{
			if (IsActive == false || IsShrinking == true)
				return;
			if (Object.HasStateAuthority == false)
				return;
			if (IsFinished == true)
				return;

			_shrinkStartTick = Runner.Simulation.Tick;
			_shrinkEndTick   = _shrinkStartTick + Mathf.CeilToInt(_shrinkDuration / Runner.DeltaTime);

			IsShrinking    = true;
			_nextShrinking = default;
		}

		private void UpdateShrinking()
		{
			var currentTick = Runner.Simulation.Tick;

			if (currentTick >= _shrinkEndTick)
			{
				IsAnnounced = false;
				IsShrinking = false;
				Radius     = ShrinkRadius;
				Center     = ShrinkCenter;

				if (_currentStage < _shrinkSteps)
				{
					SetNextShrinkingTimer();
				}
				return;
			}

			float progress = (currentTick - _shrinkStartTick) / (float)(_shrinkEndTick - _shrinkStartTick);

			var previousStage = _calculatedStages[_currentStage - 1];

			Radius = Mathf.Lerp(previousStage.Radius, ShrinkRadius,  progress);
			Center = Vector3.Lerp(new Vector3(previousStage.Center.x, transform.position.y, previousStage.Center.y), ShrinkCenter, progress);
		}

		private void UpdateDamage()
		{
			var radiusSqr = Radius * Radius;

			foreach (var player in Context.NetworkGame.Players)
			{
				if (player == null)
					continue;

				var agent = player.ActiveAgent;
				if (agent == null)
					continue;

				var agentPosition = agent.transform.position;
				var direction    = agentPosition - Center;
				direction.y      = 0f;

				if (radiusSqr > direction.sqrMagnitude)
					continue;

				var hitData = new HitData()
				{
					Action    = EHitAction.Damage,
					HitType   = EHitType.ShrinkingArea,
					Amount    = _damagePerTick,
					Position  = agentPosition,
					Target    = agent.Health,
					Normal    = Vector3.up,
				};

				HitUtility.ProcessHit(ref hitData);
			}

			_damageTimer = TickTimer.CreateFromSeconds(Runner, _damageTickTime);
		}

		private void SetNextShrinkingTimer(float additionalTime = 0f)
		{
			int playerCount = Context.NetworkGame.GetActivePlayerCount();
			ShrinkDelay = MathUtility.Map(_minShrinkDelayPlayers, _maxShrinkDelayPlayers, _minShrinkDelay, _maxShrinkDelay, playerCount);

			ShrinkDelay += additionalTime;

			_nextShrinking = TickTimer.CreateFromSeconds(Runner, ShrinkDelay);
		}

		private void CalculateStages(Vector2 endCenter)
		{
			_calculatedStages = new AreaStage[_shrinkSteps + 1];

			Vector2 startCenter = new Vector2(transform.position.x, transform.position.z);

			Vector2 center = endCenter;
			float radius = _endRadius;

			// Last stage is fixed
			_calculatedStages[_shrinkSteps] = new AreaStage(center, radius);

			for (int i = _shrinkSteps - 1; i >= 0; i--)
			{
				float nextRadius = Mathf.Lerp(_startRadius, _endRadius, i / (float)_shrinkSteps);

				Vector2 nextCenter = Vector2.zero;
				bool success = false;

				float maxSqrDistanceFromCenter = _mapRadius - nextRadius;
				maxSqrDistanceFromCenter *= maxSqrDistanceFromCenter;

				// Just try it few times instead of complex calculation, lol
				for (int j = 0; j < 50; j++)
				{
					nextCenter = center + Random.insideUnitCircle * (nextRadius - radius);

					if (Vector2.SqrMagnitude(nextCenter - startCenter) < maxSqrDistanceFromCenter)
					{
						success = true;
						break;
					}
				}

				if (success == false)
				{
					var direction = nextCenter - startCenter;
					nextCenter = startCenter + direction.normalized * (_mapRadius - nextRadius);
				}

				center = nextCenter;
				radius = nextRadius;
				_calculatedStages[i] = new AreaStage(center, radius);
			}
		}

		// HELPERS

		private struct AreaStage
		{
			public Vector2 Center;
			public float   Radius;

			public AreaStage(Vector2 center, float radius)
			{
				Center = center;
				Radius = radius;
			}
		}
	}
}
