using Fusion;
using UnityEngine;

namespace TPSBR
{
	public abstract class StaticPickup : NetworkBehaviour, IPickup
	{
		// PUBLIC MEMBERS

		public bool Consumed     => _consumed;
		public bool IsDisabled   => _isDisabled;
		public bool AutoDespawn  => _despawnDelay > 0f;

		public System.Action<StaticPickup> PickupConsumed;

		// PRIVATE MEMBERS

		[SerializeField]
		private AudioEffect _consumeSound;
		[SerializeField]
		private GameObject _visuals;
		[SerializeField]
		private float _despawnDelay = 2f;
		[SerializeField]
		private Transform _hudPosition;
		[SerializeField]
		private string _interactionName;
		[SerializeField]
		private string _interactionDescription;
		[SerializeField]
		private EBehaviour _startBehaviour;

		[Networked(OnChanged = nameof(OnBehaviourChanged), OnChangedTargets = OnChangedTargets.All)]
		private EBehaviour _behaviour { get; set; }
		[Networked(OnChanged = nameof(OnConsumedChanged), OnChangedTargets = OnChangedTargets.All)]
		private NetworkBool _consumed { get; set; }
		[Networked]
		private NetworkBool _isDisabled { get; set; }

		private TickTimer _despawnCooldown;
		private Collider  _collider;

		// IInteraction INTERFACE

		string  IInteraction.Name        => InteractionName;
		string  IInteraction.Description => InteractionDescription;
		Vector3 IInteraction.HUDPosition => _hudPosition != null ? _hudPosition.position : transform.position;
		bool    IInteraction.IsActive    => IsDisabled == false;

		protected virtual string InteractionName        => _interactionName;
		protected virtual string InteractionDescription => _interactionDescription;

		// PUBLIC MEMBERS

		public void Refresh()
		{
			if (Object == null || Object.HasStateAuthority == false)
				return;

			_despawnCooldown = default;
			_consumed        = false;

			SetIsDisabled(false);
		}

		public void SetIsDisabled(bool value)
		{
			if (Object == null || Object.HasStateAuthority == false)
				return;

			_isDisabled = value;
		}

		public bool TryConsume(Agent agent, out string result)
		{
			if (Object == null)
			{
				result = "No network state";
				return false;
			}

			if (_isDisabled == true || _consumed == true)
			{
				result = "Invalid Pickup";
				return false;
			}

			if (Consume(agent, out result) == false)
				return false;

			_consumed = true;

			if (_despawnDelay > 0f)
			{
				_despawnCooldown = TickTimer.CreateFromSeconds(Runner, _despawnDelay);
			}

			PickupConsumed?.Invoke(this);

			return true;
		}

		public void SetBehaviour(EBehaviour behaviour, float despawnDelay)
		{
			if (Object == null || Object.HasStateAuthority == false)
				return;

			_despawnDelay = despawnDelay;
			_behaviour    = behaviour;
		}

		// PROTECTED METHODS

		protected virtual bool Consume(Agent agent, out string result) { result = string.Empty; return false; }

		protected virtual void OnConsumed()
		{
			if (Runner.Stage == SimulationStages.Resimulate)
				return;

			_visuals.SetActiveSafe(false);

			if (_consumeSound != null)
			{
				_consumeSound.Play();
			}
		}

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			if (Object.HasStateAuthority == true)
			{
				_behaviour = _startBehaviour;
			}

			UpdateState();
		}

		public override void FixedUpdateNetwork()
		{
			if (Object.HasStateAuthority == false)
				return;

			if (_consumed == true && _despawnCooldown.Expired(Runner) == true)
			{
				Runner.Despawn(Object);
			}
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			PickupConsumed = null;
			_despawnCooldown = default;
		}

		// MONOBEHAVIOUR

		protected void Awake()
		{
			_collider = GetComponentInChildren<Collider>();
		}

		protected void OnTriggerEnter(Collider other)
		{
			if (Object == null || Object.HasStateAuthority == false)
				return;

			if (_consumed == true)
				return;

			var agent = other.GetComponentInParent<Agent>();
			if (agent == null)
				return;

			TryConsume(agent, out string result);
		}

		// PRIVATE METHODS

		private void UpdateState()
		{
			_collider.enabled = _consumed == false;
			_visuals.SetActiveSafe(_consumed == false);

			_collider.isTrigger = _behaviour == EBehaviour.Trigger;
			_collider.gameObject.layer = _behaviour == EBehaviour.Trigger ? ObjectLayer.Pickup : ObjectLayer.Interaction;
		}

		// NETWORK CALLBACKS

		public static void OnConsumedChanged(Changed<StaticPickup> changed)
		{
			if (changed.Behaviour._consumed == true)
			{
				changed.Behaviour.OnConsumed();
			}

			changed.Behaviour.UpdateState();
		}

		private static void OnBehaviourChanged(Changed<StaticPickup> changed)
		{
			changed.Behaviour.UpdateState();
		}

		// HELPERS

		public enum EBehaviour
		{
			None,
			Trigger,
			Interaction,
		}
	}
}
