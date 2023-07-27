using UnityEngine;
using Fusion;

namespace TPSBR
{
	public class ThrowableWeapon : FirearmWeapon, IDynamicPickupProvider
	{
		// PRIVATE MEMBERS

		[Header("Throwable")]
		[SerializeField]
		private KinematicProjectile _projectile;
		[SerializeField]
		private float _projectileSpeed = 100f;
		[SerializeField]
		private GameObject _dummyLoadedProjectile;
		[SerializeField]
		private float _minProjectileDespawnTime = 0.5f;
		[SerializeField]
		private AudioSetup _armSound;

		[Networked(OnChanged = nameof(OnArmChanged), OnChangedTargets = OnChangedTargets.All)]
		private int _armStartTick { get; set; }

		// PUBLIC MEMBERS

		public void ArmProjectile()
		{
			_armStartTick = Runner.Simulation.Tick;
		}

		// FirearmWeapon INTERFACE

		protected override bool FireProjectile(Vector3 firePosition, Vector3 targetPosition, Vector3 direction, float distanceToTarget, LayerMask hitMask, bool isFirst)
		{
			if (Object.IsProxy == true)
				return false;

			var ownerVelocity = Owner != null ? Owner.Character.CharacterController.FixedData.RealVelocity : Vector3.zero;
			if (ownerVelocity.y < 0f)
			{
				ownerVelocity.y = 0f;
			}

			// Create unique prediction key
			var predictionKey = new NetworkObjectPredictionKey
			{
				Byte0 = (byte)Runner.Simulation.Tick, // Low number part is enough
				Byte1 = (byte)Object.InputAuthority.PlayerId,
				Byte2 = (byte)Object.Id.Raw,
			};

			var projectile = Runner.Spawn(_projectile, firePosition, Quaternion.LookRotation(direction), Object.InputAuthority, BeforeProjectileSpawned, predictionKey);

			if (projectile == null)
				return true;

			projectile.Fire(Owner, firePosition, direction * _projectileSpeed + ownerVelocity, hitMask, HitType);

			float armedTime = (Runner.Simulation.Tick - _armStartTick) * Runner.DeltaTime;
			projectile.SetDespawnCooldown(Mathf.Max(_minProjectileDespawnTime, projectile.FireDespawnTime - armedTime));

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

		// IPickupProvider INTERFACE

		string IDynamicPickupProvider.Description => GetPickupDescription();
		float IDynamicPickupProvider.DespawnTime => WeaponAmmo == 0 && MagazineAmmo == 0 ? 1f : 60f;

		// PRIVATE MEMBERS

		private string GetPickupDescription()
		{
			// For prefab read initial data
			if (gameObject.scene.rootCount == 0)
				return $"Amount {_initialAmmo}";

			return $"Amount {MagazineAmmo + WeaponAmmo}";
		}

		// NETWORK CALLBACKS

		public static void OnArmChanged(Changed<ThrowableWeapon> changed)
		{
			changed.Behaviour.PlayLocalSound(changed.Behaviour._armSound);
		}
	}
}
