namespace TPSBR
{
	using UnityEngine;
	using Fusion;
	using Fusion.Plugin;

	/// <summary>
	/// This component serves as an AoI position source proxy. It is useful for objects which don't need their own NetworkTransform component, but still need to be filtered by AoI.
	/// Every frame, the component stores position of a proxy object (usually owner/parent of this object) to [Networked] Position property with a special property group.
	/// This property is never synchronized to clients and is updated on server/host only. This way AoI works correctly with zero network traffic overhead.
	/// Example: Player + Weapon. Each Weapon is a separate dynamically spawned NetworkObject with NetworkAreaOfInterestProxy component. Upon spawn Weapon is parented under Player object (usually under a hand bone).
	/// There is no need to synchronize Weapon position because it is driven locally. But if a Player runs out of AoI, we want to stop updating both objects Player and Weapon.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed unsafe class NetworkAreaOfInterestProxy : NetworkAreaOfInterestBehaviour
	{
		// PUBLIC MEMBERS

		public Transform PositionSource => _positionSource;

		// PRIVATE MEMBERS

		[SerializeField, Tooltip("If left empty, component will try to find position source in parents")]
		private Transform _positionSource;

		[Networked("NoSync")][Accuracy(AccuracyDefaults.POSITION)][HideInInspector]
		private Vector3 Position { get; set; }

		private Transform _defaultPositionSource;
		private bool      _hasExplicitPositionSource;
		private bool      _isSpawned;

		// PUBLIC METHODS

		public void SetPosition(Vector3 position)
		{
			if (_isSpawned == true)
			{
				Position = position;
			}
		}

		public void SetPositionSource(Transform positionSource)
		{
			_positionSource = positionSource;
			_hasExplicitPositionSource = true;

			Synchronize();
		}

		public void ResetPositionSource()
		{
			_positionSource = _defaultPositionSource;
			_hasExplicitPositionSource = false;

			Synchronize();
		}

		public void FindPositionSourceInParent()
		{
			FindPositionSourceInParent(true);
			Synchronize();
		}

		public void Synchronize()
		{
			if (_isSpawned == true && _positionSource != null)
			{
				Position = _positionSource.position;
			}
		}

		// NetworkAreaOfInterestBehaviour INTERFACE

		public override int PositionWordOffset => 0;

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			_isSpawned = true;

			if (_hasExplicitPositionSource == false && _positionSource == null)
			{
				Position = default;

				FindPositionSourceInParent(false);
			}

			Synchronize();
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			_isSpawned = false;

			ResetPositionSource();
		}

		public override void FixedUpdateNetwork()
		{
			Synchronize();
		}

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			_defaultPositionSource = _positionSource;
		}

		// PRIVATE METHODS

		private void FindPositionSourceInParent(bool isExplicit)
		{
			Transform parentTransform = transform.parent;
			while (parentTransform != null)
			{
				NetworkObject networkObject = parentTransform.GetComponent<NetworkObject>();
				if (networkObject != null)
				{
					NetworkAreaOfInterestBehaviour positionSource = networkObject.GetAOIPositionSource();
					if (positionSource != null)
					{
						_positionSource = positionSource.transform;
						_hasExplicitPositionSource = isExplicit;
						break;
					}
				}

				parentTransform = parentTransform.parent;
			}
		}
	}
}
