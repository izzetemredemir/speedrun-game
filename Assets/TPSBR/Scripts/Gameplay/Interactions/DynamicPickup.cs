using Fusion;
using UnityEngine;
using Plugins.Outline;

namespace TPSBR
{
	public interface IDynamicPickupProvider
	{
		public string     Name                { get; }
		public string     Description         { get; }
		public Transform  InterpolationTarget { get; }
		public Collider   Collider            { get; }
		public float      DespawnTime         { get; }
	}

	[RequireComponent(typeof(NetworkRigidbody))]
	public class DynamicPickup : ContextBehaviour, IPickup
	{
		// PUBLIC MEMBERS

		public IDynamicPickupProvider Provider { get; private set; }

		// PRIVATE MEMBERS

		[SerializeField]
		private float _despawnTime = 60f;
		[SerializeField]
		private float _fullOutlineDistance = 5f;
		[SerializeField]
		private float _noOutlineDistance = 20f;

		[Networked]
		private NetworkId _dynamicObjectID { get; set; }
		[Networked]
		private TickTimer _despawnCooldown { get; set; }

		private NetworkObject _dynamicObject;

		private NetworkRigidbody _networkRigidbody;

		private string _name;
		private string _description;

		private OutlineBehaviour _outline;
		private Color _defaultOutlineColor;
		private Color _noOutlineColor;

		// PUBLIC MEMBERS

		public void AssignObject(NetworkId dynamicObjectID)
		{
			_dynamicObjectID = dynamicObjectID;

			_dynamicObject = Runner.FindObject(_dynamicObjectID);

			if (_dynamicObject == null)
				return;

			_dynamicObject.transform.SetParent(transform, false);

			NetworkAreaOfInterestProxy aoiProxy = _dynamicObject.GetComponent<NetworkAreaOfInterestProxy>();
			if (aoiProxy != null)
			{
				aoiProxy.SetPositionSource(_networkRigidbody.transform);
			}

			Provider = _dynamicObject.GetComponent<IDynamicPickupProvider>();

			_name = Provider.Name;
			_description = Provider.Description;

			float despawnTime = Provider.DespawnTime > 0f ? Provider.DespawnTime : _despawnTime;
			_despawnCooldown = TickTimer.CreateFromSeconds(Runner, despawnTime);

			Provider.Collider.enabled = true;
			_networkRigidbody.InterpolationTarget = Provider.InterpolationTarget;
		}

		public void UnassignObject()
		{
			if (_dynamicObject.transform.parent == transform && Runner.IsShutdown == false)
			{
				_dynamicObject.transform.SetParent(null, false);

				NetworkAreaOfInterestProxy aoiProxy = _dynamicObject.GetComponent<NetworkAreaOfInterestProxy>();
				if (aoiProxy != null)
				{
					aoiProxy.SetPositionSource(null);
				}
			}

			Provider.Collider.enabled = false;

			var interpolationTarget = _networkRigidbody.InterpolationTarget;
			if (interpolationTarget != null)
			{
				interpolationTarget.localPosition = Vector3.zero;
				interpolationTarget.localRotation = Quaternion.identity;

				_networkRigidbody.InterpolationTarget = null;
			}

			_dynamicObject = null;
			_dynamicObjectID = default;
			Provider = null;

			_despawnCooldown = TickTimer.CreateFromSeconds(Runner, 1f);
		}

		// NetworkObject INTERFACE

		public override void Spawned()
		{
			if (_despawnCooldown.IsRunning == false)
			{
				_despawnCooldown = _despawnTime > 0f ? TickTimer.CreateFromSeconds(Runner, _despawnTime) : default;
			}
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			if (_dynamicObject != null)
			{
				var objectToDespawn = _dynamicObjectID.IsValid == true ? _dynamicObject : null;

				UnassignObject();

				if (objectToDespawn != null)
				{
					Runner.Despawn(objectToDespawn);
				}
			}

			_dynamicObjectID = default;

			_networkRigidbody.Rigidbody.velocity = Vector3.zero;
			_networkRigidbody.Rigidbody.angularVelocity = Vector3.zero;
		}

		public override void FixedUpdateNetwork()
		{
			if (Object.HasStateAuthority && _despawnCooldown.Expired(Runner) == true)
			{
				Runner.Despawn(Object);
				return;
			}

			if (_dynamicObjectID.IsValid == true && _dynamicObject == null)
			{
				AssignObject(_dynamicObjectID);
			}

			if (_dynamicObjectID.IsValid == false && _dynamicObject != null)
			{
				UnassignObject();
			}
		}

		// MONOBEHAVIOUR

		protected void Awake()
		{
			_networkRigidbody = GetComponent<NetworkRigidbody>();
			_outline = GetComponent<OutlineBehaviour>();

			if (_outline != null)
			{
				_defaultOutlineColor = _outline.Settings.Color;
				_noOutlineColor = _defaultOutlineColor;
				_noOutlineColor.a = 0f;
			}
		}

		protected void Update()
		{
			UpdateOutline();
		}

		// IPickup INTERFACE

		string  IInteraction.Name        => _name;
		string  IInteraction.Description => _description;
		Vector3 IInteraction.HUDPosition => transform.position;
		bool    IInteraction.IsActive    => true;

		// PRIVATE METHODS

		private void UpdateOutline()
		{
			if (Runner == null)
				return;

			if (Runner.IsPlayer == false)
				return;

			if (_outline == null)
				return;

			if (_dynamicObject == null)
				return;

			var agent = Context.ObservedAgent;
			if (agent == null)
				return;

			float sqrDistance = (agent.transform.position - transform.position).sqrMagnitude;

			if (sqrDistance > _noOutlineDistance * _noOutlineDistance)
			{
				_outline.enabled = false;
				return;
			}

			float distance = Mathf.Sqrt(sqrDistance);
			float progress = (distance - _fullOutlineDistance) / (_noOutlineDistance - _fullOutlineDistance);

			_outline.enabled = true;
			_outline.Settings.Color = Color.Lerp(_defaultOutlineColor, _noOutlineColor, progress);
		}
	}
}
