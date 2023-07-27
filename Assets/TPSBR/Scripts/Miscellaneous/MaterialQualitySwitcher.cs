using UnityEngine;

namespace TPSBR
{
	public class MaterialQualitySwitcher : MonoBehaviour
	{
		[SerializeField]
		private Renderer _renderer;
		[SerializeField]
		private Material _low;
		[SerializeField]
		private Material _medium;
		[SerializeField]
		private Material _high;
		[SerializeField]
		private Material _ultra;

		private int _qualityLevel = -1;

		// MONOBEHAVIOUR

		private void Awake()
		{
			UpdateMaterial();
		}

		private void Update()
		{
			UpdateMaterial();
		}

		// PRIVATE METHODS

		private void UpdateMaterial()
		{
			int qualityLevel = QualitySettings.GetQualityLevel();
			if (_qualityLevel == qualityLevel)
				return;

			_qualityLevel = qualityLevel;

			switch (qualityLevel)
			{
				case 0: _renderer.material = _low;    break;
				case 1: _renderer.material = _medium; break;
				case 2: _renderer.material = _high;   break;
				case 3: _renderer.material = _ultra;  break;
			}
		}
	}
}
