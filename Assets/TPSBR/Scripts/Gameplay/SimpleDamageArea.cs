using Fusion;
using UnityEngine;

namespace TPSBR
{
	public class SimpleDamageArea : NetworkBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private float _radius = 1f;
		[SerializeField]
		private float _damagePerSecond = 20f;
		[SerializeField]
		private int _hitsPerSecond = 4;
		[SerializeField]
		private LayerMask _hitMask;

		[Networked]
		private TickTimer _cooldown { get; set; }

		private Collider[] _hitColliders = new Collider[6];

		// NetworkBehaviour INTERFACE

		public override void FixedUpdateNetwork()
		{
			if (Object.HasStateAuthority == false)
				return;

			if (_damagePerSecond <= 0f)
				return;

			if (_cooldown.ExpiredOrNotRunning(Runner) == true)
			{
				Fire();
			}
		}

		// PRIVATE METHODS

		private void Fire()
		{
			int hits = Runner.SimulationUnityScene.GetPhysicsScene().OverlapSphere(transform.position, _radius, _hitColliders, _hitMask, QueryTriggerInteraction.UseGlobal);
			_cooldown = TickTimer.CreateFromSeconds(Runner, 1f / _hitsPerSecond);

			if (hits == 0)
				return;

			float damage = _damagePerSecond  / _hitsPerSecond;

			for (int i = 0; i < hits; i++)
			{
				HitUtility.ProcessHit(this, _hitColliders[i], damage, EHitType.Suicide, out HitData hit);
			}
		}
	}
}
