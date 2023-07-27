using System;
using Fusion;
using UnityEngine;

namespace TPSBR
{
	[Serializable]
	public class ProjectileDamage
	{
		public float Damage             = 10f;
		public float MaxDistance        = 300f;
		public float FullDamageDistance = 80f;

		public float GetDamage(float distance)
		{
			if (distance < FullDamageDistance)
				return Damage;

			if (FullDamageDistance >= MaxDistance)
				return Damage;

			return Mathf.Lerp(Damage, 0f, (distance - FullDamageDistance) / (MaxDistance - FullDamageDistance));
		}
	}

	public abstract class Projectile : ContextBehaviour, IPredictedSpawnBehaviour
	{
		// PUBLIC MEMBERS

		public PlayerRef PredictedInputAuthority { get; set; }

		public bool      IsPredicted    => Object == null || Object.IsPredictedSpawn;
		public PlayerRef InputAuthority => IsPredicted == true ? PredictedInputAuthority : Object.InputAuthority;

		// PUBLIC METHODS

		public abstract void Fire(Agent owner, Vector3 firePosition, Vector3 initialVelocity, LayerMask hitMask, EHitType hitType);

		// IPredictedSpawnBehaviour INTERFACE

		void IPredictedSpawnBehaviour.PredictedSpawnSpawned()
		{
			Spawned();
		}

		void IPredictedSpawnBehaviour.PredictedSpawnUpdate()
		{
			FixedUpdateNetwork();
		}

		void IPredictedSpawnBehaviour.PredictedSpawnRender()
		{
			Render();
		}

		void IPredictedSpawnBehaviour.PredictedSpawnFailed()
		{
			Despawned(Runner, false);

			Runner.Despawn(Object, true);
		}

		void IPredictedSpawnBehaviour.PredictedSpawnSuccess()
		{
			// Nothing special is needed
		}

		// NetworkBehaviour INTERFACE

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			PredictedInputAuthority = PlayerRef.None;
		}
	}
}
