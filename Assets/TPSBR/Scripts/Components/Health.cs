using System;
using Fusion;

namespace TPSBR
{
	using UnityEngine;

	public struct BodyHitData : INetworkStruct
	{
		public EHitAction Action;
		public float      Damage;
		[Networked, Accuracy(0.01f)]
		public Vector3    RelativePosition { get; set; }
		[Networked, Accuracy(0.01f)]
		public Vector3    Direction { get; set; }
		public PlayerRef  Instigator;
	}

	public class Health : ContextBehaviour, IHitTarget, IHitInstigator
	{
		// PUBLIC MEMBERS

		public bool  IsAlive   => CurrentHealth > 0f;
		public float MaxHealth => _maxHealth;
		public float MaxShield => _maxShield;

		[Networked, HideInInspector]
		public float CurrentHealth { get; private set; }
		[Networked, HideInInspector]
		public float CurrentShield { get; private set; }

		public event Action<HitData> HitTaken;
		public event Action<HitData> HitPerformed;

		// PRIVATE MEMBERS

		[SerializeField]
		private float _maxHealth;
		[SerializeField]
		private float _maxShield;
		[SerializeField]
		private float _startShield;
		[SerializeField]
		private Transform _hitIndicatorPivot;

		[Header("Regeneration")]
		[SerializeField]
		private float _healthRegenPerSecond;
		[SerializeField]
		private float _maxHealthFromRegen;
		[SerializeField]
		private int _regenTickPerSecond;
		[SerializeField]
		private int _regenCombatDelay;

		[Networked]
		private int _hitCount { get; set; }
		[Networked, Capacity(8)]
		private NetworkArray<BodyHitData> _hitData { get; }

		private int _visibleHitCount;
		private Agent _agent;

		private TickTimer _regenTickTimer;
		private float _healthRegenPerTick;
		private float _regenTickTime;

		// PUBLIC METHODS

		public void OnSpawned(Agent agent)
		{
			_visibleHitCount = _hitCount;
		}

		public void OnDespawned()
		{
			HitTaken = null;
			HitPerformed = null;
		}

		public void OnFixedUpdate()
		{
			if (Object.HasStateAuthority == false)
				return;

			if (IsAlive == true && _healthRegenPerSecond > 0f && _regenTickTimer.ExpiredOrNotRunning(Runner) == true)
			{
				_regenTickTimer = TickTimer.CreateFromSeconds(Runner, _regenTickTime);

				var healthDiff = _maxHealthFromRegen - CurrentHealth;
				if (healthDiff <= 0f)
					return;

				AddHealth(Mathf.Min(healthDiff, _healthRegenPerTick));
			}
		}

		public void ResetRegenDelay()
		{
			_regenTickTimer = TickTimer.CreateFromSeconds(Runner, _regenCombatDelay);
		}

		public override void CopyBackingFieldsToState(bool firstTime)
		{
			base.CopyBackingFieldsToState(firstTime);

			InvokeWeavedCode();

			CurrentHealth = _maxHealth;
			CurrentShield = _startShield;
		}

		// NetworkBehaviour INTERFACE

		public override void Render()
		{
			if (Runner.Simulation.Mode != SimulationModes.Server)
			{
				UpdateVisibleHits();
			}
		}

		// MONOBEHAVIOUR

		protected void Awake()
		{
			_agent = GetComponent<Agent>();

			_regenTickTime      = 1f / _regenTickPerSecond;
			_healthRegenPerTick = _healthRegenPerSecond / _regenTickPerSecond;
		}

		// IHitTarget INTERFACE

		Transform IHitTarget.HitPivot => _hitIndicatorPivot != null ? _hitIndicatorPivot : transform;

		void IHitTarget.ProcessHit(ref HitData hitData)
		{
			if (IsAlive == false)
			{
				hitData.Amount = 0;
				return;
			}

			ApplyHit(ref hitData);

			if (IsAlive == false)
			{
				hitData.IsFatal = true;
				Context.GameplayMode.AgentDeath(_agent, hitData);
			}
		}

		// IHitInstigator INTERFACE

		void IHitInstigator.HitPerformed(HitData hitData)
		{
			if (hitData.Amount > 0 && hitData.Target != (IHitTarget)this && Runner.IsResimulation == false)
			{
				HitPerformed?.Invoke(hitData);
			}
		}

		// PRIVATE METHODS

		private void ApplyHit(ref HitData hit)
		{
			if (IsAlive == false)
				return;

			if (hit.Action == EHitAction.Damage)
			{
				hit.Amount = ApplyDamage(hit.Amount);
			}
			else if (hit.Action == EHitAction.Heal)
			{
				hit.Amount = AddHealth(hit.Amount);
			}
			else if (hit.Action == EHitAction.Shield)
			{
				hit.Amount = AddShield(hit.Amount);
			}

			if (hit.Amount <= 0)
				return;

			// Hit taken effects (blood) is shown immediately for local player, for other
			// effects (hit number, crosshair hit effect) we are waiting for server confirmation
			if (hit.InstigatorRef == Context.LocalPlayerRef && Runner.IsForward == true)
			{
				HitTaken?.Invoke(hit);
			}

			if (Object.HasStateAuthority == false)
				return;

			_hitCount++;

			var bodyHitData = new BodyHitData
			{
				Action           = hit.Action,
				Damage           = hit.Amount,
				Direction        = hit.Direction,
				RelativePosition = hit.Position != Vector3.zero ? hit.Position - transform.position : Vector3.zero,
				Instigator       = hit.InstigatorRef,
			};

			int hitIndex = _hitCount % _hitData.Length;
			_hitData.Set(hitIndex, bodyHitData);
		}

		private float ApplyDamage(float damage)
		{
			if (damage <= 0f)
				return 0f;

			ResetRegenDelay();

			var shieldChange = AddShield(-damage);
			var healthChange = AddHealth(-(damage + shieldChange));

			return -(shieldChange + healthChange);
		}

		private float AddHealth(float health)
		{
			float previousHealth = CurrentHealth;
			SetHealth(CurrentHealth + health);
			return CurrentHealth - previousHealth;
		}

		private float AddShield(float shield)
		{
			float previousShield = CurrentShield;
			SetShield(CurrentShield + shield);
			return CurrentShield - previousShield;
		}

		private void SetHealth(float health)
		{
			CurrentHealth = Mathf.Clamp(health, 0, _maxHealth);
		}

		private void SetShield(float shield)
		{
			CurrentShield = Mathf.Clamp(shield, 0, _maxShield);
		}

		private void UpdateVisibleHits()
		{
			if (_visibleHitCount == _hitCount)
				return;

			int dataCount = _hitData.Length;
			int oldestHitData = _hitCount - dataCount + 1;

			for (int i = Mathf.Max(_visibleHitCount + 1, oldestHitData); i <= _hitCount; i++)
			{
				int shotIndex = i % dataCount;
				var bodyHitData = _hitData.Get(shotIndex);

				var hitData = new HitData
				{
					Action        = bodyHitData.Action,
					Amount        = bodyHitData.Damage,
					Position      = transform.position + bodyHitData.RelativePosition,
					Direction     = bodyHitData.Direction,
					Normal        = -bodyHitData.Direction,
					Target        = this,
					InstigatorRef = bodyHitData.Instigator,
					IsFatal       = i == _hitCount && CurrentHealth <= 0f,
				};

				OnHitTaken(hitData);
			}

			_visibleHitCount = _hitCount;
		}

		private void OnHitTaken(HitData hit)
		{
			// For local player, HitTaken was already called when applying hit
			if (hit.InstigatorRef != Context.LocalPlayerRef)
			{
				HitTaken?.Invoke(hit);
			}

			// We use _hitData buffer to inform instigator about successful hit as this needs
			// to be synchronized over network as well (e.g. when spectating other players)
			if (hit.InstigatorRef.IsValid == true && hit.InstigatorRef == Context.ObservedPlayerRef)
			{
				var instigator = hit.Instigator;

				if (instigator == null)
				{
					var player = Context.NetworkGame.GetPlayer(hit.InstigatorRef);
					instigator = player != null ? player.ActiveAgent.Health as IHitInstigator : null;
				}

				if (instigator != null)
				{
					instigator.HitPerformed(hit);
				}
			}
		}

		// DEBUG

		[ContextMenu("Add Health")]
		private void Debug_AddHealth()
		{
			CurrentHealth += 10;
		}

		[ContextMenu("Remove Health")]
		private void Debug_RemoveHealth()
		{
			CurrentHealth -= 10;
		}
	}
}
