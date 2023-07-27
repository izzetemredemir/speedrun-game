using Fusion;

namespace TPSBR
{
	using UnityEngine;

	public class Explosion : ContextBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private LayerMask  _hitMask;
		[SerializeField]
		private EHitType   _hitType;

		[SerializeField]
		private float      _innerRadius;
		[SerializeField]
		private float      _outerRadius;

		[SerializeField]
		private float      _innerHitValue;
		[SerializeField]
		private float      _outerHitValue;

		[SerializeField]
		private bool       _useBodyPartMultipliers;

		[SerializeField]
		private float      _despawnDelay;

		[SerializeField]
		private Transform  _effectRoot;


		private TickTimer  _despawnTimer;

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			base.Spawned();

			ShowEffect();
			Explode();
		}

		public override void FixedUpdateNetwork()
		{
			if (Object.HasStateAuthority == false)
				return;
			if (_despawnTimer.Expired(Runner) == false)
				return;

			Runner.Despawn(Object);
		}

		// PRIVATE METHODS

		private void Explode()
		{
			if (Object.HasStateAuthority == false)
				return;

			var hits = ListPool.Get<LagCompensatedHit>(16);
			var hitRoots = ListPool.Get<int>(16);

			var position = transform.position + Vector3.up * 0.5f; // Check explosion slightly above

			int count = Runner.LagCompensation.OverlapSphere(position, _outerRadius, Object.InputAuthority, hits, _hitMask);
			bool damageFalloff = _innerRadius < _outerRadius && _innerHitValue != _outerHitValue;

			var player = Context.NetworkGame.GetPlayer(Object.InputAuthority);
			var owner = player != null ? player.ActiveAgent : null;

			for (int i = 0; i < count; i++)
			{
				var hit = hits[i];

				if (hit.Hitbox == null)
					continue;

				var hitTarget = hit.Hitbox.Root.GetComponent<IHitTarget>();

				if (hitTarget == null)
					continue;

				int hitRootID = hit.Hitbox.Root.GetInstanceID();
				if (hitRoots.Contains(hitRootID) == true)
					continue; // Same object was hit multiple times

				// TODO: Replace this when detailed hit info will be fixed
				var direction = hit.GameObject.transform.position - position;
				float distance = direction.magnitude;
				direction /= distance; // Normalize

				if (Runner.GetPhysicsScene().Raycast(position, direction, distance, ObjectLayerMask.Default) == true)
					continue;

				hitRoots.Add(hitRootID);

				float damage = _innerHitValue;

				if (damageFalloff == true && distance > _innerRadius)
				{
					damage = MathUtility.Map(_innerRadius, _outerRadius, _innerHitValue, _outerHitValue, distance);
				}

				// TODO: Remove this when detailed hit info will be fixed
				hit.Point = hit.GameObject.transform.position;
				hit.Normal = -direction;

				if (owner != null)
				{
					HitUtility.ProcessHit(owner, direction, hit, damage, _hitType, out HitData hitData);
				}
				else
				{
					HitUtility.ProcessHit(Object.InputAuthority, direction, hit, damage, _hitType, out HitData hitData);
				}
			}

			ListPool.Return(hitRoots);
			ListPool.Return(hits);

			_despawnTimer = TickTimer.CreateFromSeconds(Runner, _despawnDelay);
		}

		private void ShowEffect()
		{
			if (Runner.Mode == SimulationModes.Server)
				return;

			if (_effectRoot != null)
			{
				_effectRoot.SetActive(true);
				_effectRoot.localScale = Vector3.one * _outerRadius * 2f;
			}
		}
	}
}
