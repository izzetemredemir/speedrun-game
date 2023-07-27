using UnityEngine;

namespace TPSBR
{
	public class DestroyAfter : MonoBehaviour
	{
		[SerializeField]
		private float _delay = 3f;

		private float _time;

		protected void Update()
		{
			_time += Time.deltaTime;

			if (_time >= _delay)
			{
				Destroy(gameObject);
			}
		}
	}
}