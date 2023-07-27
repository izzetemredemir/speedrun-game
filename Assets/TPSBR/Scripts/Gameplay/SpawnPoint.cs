using UnityEngine;

namespace TPSBR
{
	public class SpawnPoint : MonoBehaviour
	{
		public bool SpawnEnabled = true;

		private void OnDrawGizmosSelected()
		{
			Gizmos.DrawWireSphere(transform.position, 2.0f);
		}
	}
}
