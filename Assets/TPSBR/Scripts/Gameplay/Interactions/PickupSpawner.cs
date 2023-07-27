using UnityEngine;
using Fusion;

namespace TPSBR
{
	public class PickupSpawner : NetworkBehaviour
	{
		[SerializeField]
		private Transform _spawnPoint;
		[SerializeField]
		private StaticPickup[] _pickupPrefabs;
		[SerializeField]
		private float _refillTime = 30;

		[Networked]
		private TickTimer _refillCooldown { get; set; }
		[Networked]
		private StaticPickup _activePickup { get; set; }

		// NetworkBehaviour INTERFACE

		public override void FixedUpdateNetwork()
		{
			if (Object.HasStateAuthority == false)
				return;

			if (_activePickup != null)
			{
				if (_activePickup.Object.IsValid == true && _activePickup.Consumed == false)
					return;

				_activePickup = null;
				_refillCooldown = TickTimer.CreateFromSeconds(Runner, _refillTime);
				return;
			}

			if (_refillCooldown.ExpiredOrNotRunning(Runner) == false)
				return;

			var prefab = _pickupPrefabs[Random.Range(0, _pickupPrefabs.Length)];
			_activePickup = Runner.Spawn(prefab, _spawnPoint.position, _spawnPoint.rotation);
		}
	}
}
