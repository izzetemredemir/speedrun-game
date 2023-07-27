using UnityEngine;

namespace TPSBR
{
	public class AgentVFX : MonoBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private GameObject _hit;
		[SerializeField]
		private GameObject _criticalHit;

		[SerializeField]
		private AudioEffect[] _soundEffects;

		private Agent _agent;

		// PUBLIC MEMBERS

		public void OnSpawned(Agent agent)
		{
			var health = GetComponent<Health>();
			health.HitTaken += OnHitTaken;

			_agent = agent;
		}

		public void OnDespawned()
		{
			var health = GetComponent<Health>();
			if (health != null)
			{
				health.HitTaken -= OnHitTaken;
			}
		}

		public void PlaySound(AudioSetup sound, EForceBehaviour force = EForceBehaviour.None)
		{
			if (ApplicationSettings.IsStrippedBatch == true)
				return;

			_soundEffects.PlaySound(sound, force);
		}

		// PRIVATE METHODS

		private void OnHitTaken(HitData hit)
		{
			if (hit.Amount <= 0 || hit.Action != EHitAction.Damage)
				return;

			if (hit.Position == Vector3.zero)
				return;

			var hitPrefab = hit.IsCritical == true ? _criticalHit : _hit;
			SpawnHit(hitPrefab, hit.Position, hit.Normal);
		}

		private void SpawnHit(GameObject hitPrefab, Vector3 position, Vector3 normal)
		{
			var hit = _agent.Context.ObjectCache.Get(hitPrefab, transform);
			var rotation = normal != Vector3.zero ? Quaternion.LookRotation(normal) : Quaternion.identity;
			hit.transform.SetPositionAndRotation(position, rotation);

			_agent.Context.ObjectCache.ReturnDeferred(hit, 2f);
		}
	}
}
