namespace TPSBR
{
	using UnityEngine;

	public class GroundSlider : MonoBehaviour
	{
		public Vector3 Direction;
		public float   Speed;
		public int     Count;

		private static GroundSlider _instance;

		private void Awake()
		{
			if (_instance != null)
				return;

			_instance = this;
			_instance = Instantiate(this);

			for (int i = -Count + 1; i < Count; ++i)
			{
				for (int j = -Count + 1; j < Count; ++j)
				{
					if (i == 0 && j == 0)
						continue;

					GroundSlider slider = Instantiate(this, _instance.transform);
					slider.enabled = false;
					slider.transform.localScale = Vector3.one;
					slider.transform.localPosition = new Vector3(j, 0.0f, i);
				}
			}

			enabled = false;
			GetComponent<MeshRenderer>().enabled = false;
		}

		private void Update()
		{
			transform.position += Direction.normalized * Speed * Time.deltaTime;
		}
	}
}
