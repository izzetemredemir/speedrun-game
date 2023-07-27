using UnityEngine;
using Fusion;
using System.Collections.Generic;

namespace TPSBR
{
	public struct ProjectileData : INetworkStruct
	{
		public Vector3 Destination;
		[Networked, Accuracy(0.01f)]
		public Vector3 ImpactNormal { get; set; }
		public int     ImpactTagHash;
	}

	public class HitscanWeapon : FirearmWeapon
	{
		// PRIVATE MEMBERS

		[Header("Hitscan Setup")]
		[SerializeField]
		private DummyProjectile  _projectile;
		[SerializeField]
		private ImpactSetup      _impactSetup;
		[SerializeField]
		private float            _damageMultiplier = 1f;

		[Networked, Capacity(25)]
		private NetworkArray<ProjectileData> _projectileData { get; }

		private List<LagCompensatedHit> _validHits = new List<LagCompensatedHit>(16);

		// FirearmWeapon INTERFACE

		protected override bool FireProjectile(Vector3 firePosition, Vector3 targetPosition, Vector3 direction, float distanceToTarget, LayerMask hitMask, bool isFirst)
		{
			Vector3 impactNormal = Vector3.zero;
			Vector3 impactPosition = targetPosition;

			int impactTagHash = 0;

			int ownerObjectID = Owner != null ? Owner.gameObject.GetInstanceID() : 0;
			ProjectileUtility.ProjectileCast(Runner, Object.InputAuthority, ownerObjectID, firePosition, direction, distanceToTarget, _projectile.MaxDistance, hitMask, _validHits);

			float damagePenalty = 0f;
			float maxDamage = _projectile.GetDamage(0f);

			bool impactRegistered = false;

			for (int i = 0; i < _validHits.Count; i++)
			{
				if (damagePenalty >= maxDamage)
					break;

				var hit = _validHits[i];

				float realDistance = Vector3.Distance(firePosition, hit.Point);
				float hitDamage = _projectile.GetDamage(realDistance) * _damageMultiplier - damagePenalty;

				if (hitDamage <= 0f)
					break;

				HitUtility.ProcessHit(Owner, direction, hit, hitDamage, HitType, out HitData hitData);

				int hitTagHash = hit.GameObject.tag.GetHashCode();

				// Spawn impacts on static objects only
				if (impactRegistered == false && hit.GameObject.layer != ObjectLayer.Agent && hit.GameObject.layer != ObjectLayer.Target)
				{
					impactNormal = (hit.Normal + -direction) * 0.5f;
					impactTagHash = hitTagHash;
					impactPosition = hit.Point;

					// We want impact on first solid object
					impactRegistered = true;
				}

				float damageMultiplier = _projectile.Piercing != null ? _projectile.Piercing.GetDamageMultiplier(hitTagHash) : 0f;
				damagePenalty += maxDamage - maxDamage * damageMultiplier;
			}

			var projectileData = new ProjectileData()
			{
				Destination = impactPosition,
				ImpactNormal = impactNormal,
				ImpactTagHash = impactTagHash,
			};

			int projectileIndex = _projectilesCount % _projectileData.Length;
			//int shotIndex = _projectilesCount % _shotData.Length;
			_projectileData.Set(projectileIndex, projectileData);

			return true;
		}

		protected override void FireVisualProjectile(int projectileIndex, bool playFireEffects)
		{
			var projectileData = _projectileData[projectileIndex % _projectileData.Length];

			var projectileInstance = Context.ObjectCache.Get(_projectile);
			Runner.MoveToRunnerScene(projectileInstance);
			projectileInstance.Context = Context;

			projectileInstance.Fire(FireTransform.position, FireTransform.rotation, projectileData.Destination);

			if (projectileData.ImpactNormal != Vector3.zero)
			{
				var impactParticle = Context.ObjectCache.Get(_impactSetup.GetImpact(projectileData.ImpactTagHash));
				Context.ObjectCache.ReturnDeferred(impactParticle, 5f);
				Runner.MoveToRunnerSceneExtended(impactParticle);

				impactParticle.transform.position = projectileData.Destination;
				impactParticle.transform.rotation = Quaternion.LookRotation(projectileData.ImpactNormal);
			}

			base.FireVisualProjectile(projectileIndex, playFireEffects);
		}
	}
}
