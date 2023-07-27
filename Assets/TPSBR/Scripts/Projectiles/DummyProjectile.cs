using UnityEngine;

namespace TPSBR
{
	public class DummyProjectile : MonoBehaviour
	{
		// PUBLIC MEMBERS

		public SceneContext Context { get; set; }

		public float MaxDistance => _damage.MaxDistance;
		public PiercingSetup Piercing => _piercing;

		// PRIVATE METHODS

		[SerializeField]
		private ProjectileDamage _damage;
		[SerializeField]
		private float _speed = 40f;
		[SerializeField]
		private PiercingSetup _piercing;

		private float _time;
		private float _duration;

		private Vector3 _start;
		private Vector3 _destination;

		private int _startFrame;

		private TrailRenderer[] _lineRenderers;

		// PUBLIC METHODS

		public void Fire(Vector3 start, Quaternion rotation, Vector3 destination)
		{
			transform.position = start;
			transform.rotation = rotation;

			_start = start;
			_destination = destination;

			_duration = Vector3.Magnitude(destination - start) / _speed;
			_time = 0f;

			_startFrame = Time.frameCount;

			for (int i = 0; i < _lineRenderers.Length; i++)
			{
				_lineRenderers[i].Clear();
			}
		}

		public float GetDamage(float distance)
		{
			return _damage.GetDamage(distance);
		}

		// MONOBEHAVIOR

		private void Awake()
		{
			_lineRenderers = GetComponentsInChildren<TrailRenderer>(true);
		}

		private void Update()
		{
			if (_startFrame == Time.frameCount)
				return;

			_time += Time.deltaTime;

			if (_time >= _duration)
			{
				Context.ObjectCache.Return(this);
				return;
			}

			float progress = _time / _duration;
			transform.position = Vector3.Lerp(_start, _destination, progress);
		}
	}
}
