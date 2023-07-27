using UnityEngine;

namespace TPSBR.UI
{
	[RequireComponent(typeof(CanvasGroup))]
	public class UIHitDirectionItem : UIBehaviour
	{
		// PUBLIC MEMBERS

		public bool  IsActive        => ShowCooldown > 0f;
		public float ShowCooldown    { get; private set; }
		public float Angle           { get; private set; }

		// PRIVATE MEMBERS

		[SerializeField]
		private float _hideTime = 0.5f;

		private float _targetAlpha = 1f;

		// PUBLIC METHODS

		public void Show(float duration, float angle)
		{
			ShowCooldown = duration;
			Angle = angle;

			var rotation = RectTransform.localRotation.eulerAngles;
			rotation.z = angle;

			RectTransform.localRotation = Quaternion.Euler(rotation);

			CanvasGroup.alpha = 0f;
		}

		// MONOBEHAVIOUR

		protected void Awake()
		{
			_targetAlpha = CanvasGroup.alpha;
			CanvasGroup.alpha = 0f;
		}

		protected void Update()
		{
			if (IsActive == false)
				return;

			ShowCooldown -= Time.deltaTime;

			if (ShowCooldown <= 0f)
			{
				ShowCooldown = 0f;
				CanvasGroup.alpha = 0f;
				return;
			}

			if (ShowCooldown < _hideTime)
			{
				float progress = ShowCooldown / _hideTime;
				CanvasGroup.alpha = Mathf.Lerp(0f, _targetAlpha, progress);
			}
			else
			{
				CanvasGroup.alpha = 1f;
			}
		}
	}
}
