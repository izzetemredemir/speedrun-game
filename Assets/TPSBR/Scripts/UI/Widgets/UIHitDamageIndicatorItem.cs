using UnityEngine;
using TMPro;

namespace TPSBR.UI
{
	public class UIHitDamageIndicatorItem : UIBehaviour
	{
		public Vector3 WorldPosition => _worldPosition;
		public bool    IsFinished    => CanvasGroup.alpha <= 0f;

		[SerializeField]
		private TextMeshProUGUI _text;
		[SerializeField]
		private Vector3 _randomOffset;

		private Vector3 _worldPosition;

		public void Activate(float value, Vector3 worldPosition)
		{
			_worldPosition = worldPosition + new Vector3(Random.Range(-_randomOffset.x, _randomOffset.x), Random.Range(-_randomOffset.y, _randomOffset.y), Random.Range(-_randomOffset.z, _randomOffset.z));

			int intValue = Mathf.RoundToInt(value);

			if (intValue == 0 && value != 0f)
			{
				// Do not show zero if not necessary
				intValue = value > 0f ? 1 : -1;
			}

			_text.text = intValue.ToString();
		}
	}
}
