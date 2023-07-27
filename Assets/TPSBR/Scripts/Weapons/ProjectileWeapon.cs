using Fusion;
using UnityEngine;

namespace TPSBR
{
	public class ProjectileWeapon : FirearmWeapon
	{
		// PRIVATE MEMBERS

		[Header("Projectile")]
		[SerializeField]
		private Projectile _projectile;
		[SerializeField]
		private float _projectileSpeed = 100f;
		[SerializeField]
		private GameObject _dummyLoadedProjectile;

		// FirearmWeapon INTERFACE

		protected override bool FireProjectile(Vector3 firePosition, Vector3 targetPosition, Vector3 direction, float distanceToTarget, LayerMask hitMask, bool isFirst)
		{
			// Spawning of new network object for projectiles is fine here because we use it only for slow low cadence projectiles (grenades, arrows).
			// If you plan to have kinematic projectiles for rifles etc. you should consider using projectile data buffer similar to the one in HitscanWeapon.

			if (Object.IsProxy == true)
				return false;

			// Create unique prediction key
			var predictionKey = new NetworkObjectPredictionKey
			{
				Byte0 = (byte)Runner.Simulation.Tick, // Low number part is enough
				Byte1 = (byte)Object.InputAuthority.PlayerId,
				Byte2 = (byte)Object.Id.Raw,
			};

			var projectile = Runner.Spawn(_projectile, firePosition, Quaternion.LookRotation(direction), Object.InputAuthority, BeforeProjectileSpawned, predictionKey);

			if (projectile == null)
				return false;

			projectile.Fire(Owner, firePosition, direction * _projectileSpeed, hitMask, HitType);

			void BeforeProjectileSpawned(NetworkRunner runner, NetworkObject spawnedObject)
			{
				if (HasStateAuthority == true)
					return;

				var projectile = spawnedObject.GetComponent<Projectile>();
				projectile.PredictedInputAuthority = Object.InputAuthority;
			}

			return true;
		}

		public override void Render()
		{
			base.Render();

			_dummyLoadedProjectile.SetActiveSafe(IsReloading == false && MagazineAmmo > 0);
		}
	}
}
