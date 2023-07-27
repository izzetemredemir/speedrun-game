using UnityEngine;

namespace TPSBR.UI
{
	public class UIHitDirection : UIBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private float _visiblityTime = 4f;
		[SerializeField]
		private float _minAngleDifference = 5f;

		private UIHitDirectionItem[] _items;
		private int _maxCount;

		// PUBLIC METHODS

		public void ShowHit(HitData hit)
		{
			float angle = Vector3.SignedAngle(-hit.Direction, Vector3.forward, Vector3.up);
			var item = GetItem(ref angle);
			
			item.Show(_visiblityTime, angle);
		}

		public void UpdateDirection(Agent agent)
		{
			var rotation = RectTransform.rotation.eulerAngles;

			float angle = Vector3.SignedAngle(agent.transform.forward, Vector3.forward, Vector3.up);
			rotation.z = -angle;

			RectTransform.rotation = Quaternion.Euler(rotation);
		}

		// MONOBEHAVIOUR

		protected void Awake()
		{
			_items = GetComponentsInChildren<UIHitDirectionItem>();
			_maxCount = _items.Length;
		}

		// PRIVATE METHODS

		private UIHitDirectionItem GetItem(ref float angle)
		{
			UIHitDirectionItem bestItem = _items[0];

			for (int i = 0; i < _maxCount; i++)
			{
				var item = _items[i];

				if (item.IsActive == false)
				{
					bestItem = item;
					continue;
				}

				if (Mathf.DeltaAngle(angle, item.Angle) < _minAngleDifference)
				{
					angle = item.Angle;
					return item;
				}

				if (item.ShowCooldown < bestItem.ShowCooldown)
				{
					bestItem = item;
				}
			}

			return bestItem;
		}

		//private bool IsCloseAngle(float angleA, float angleB)
		//{
		//	if (Mathf.Abs(angleA - angleB) < _minAngleDifference)
		//		return true;

		//	float angleATo180 = GetAngleTo180(angleA);
		//	float angleBTo180 = GetAngleTo180(angleB);

		//	return false;
		//}

		//private float GetAngleTo180(float angle)
		//{
		//	return angle > 0f ? 180f - angle : Mathf.Abs(-180f - angle);
		//}
	}
}
