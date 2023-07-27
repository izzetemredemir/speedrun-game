using System.Collections.Generic;
using UnityEngine;

namespace TPSBR.UI
{
	public class UIHitDamageIndicator : UIWidget
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private UIHitDamageIndicatorItem _hitItem;
		[SerializeField]
		private UIHitDamageIndicatorItem _critItem;
		[SerializeField]
		private UIHitDamageIndicatorItem _fatalItem;

		private List<UIHitDamageIndicatorItem> _activeItems   = new List<UIHitDamageIndicatorItem>();
		private List<UIHitDamageIndicatorItem> _inactiveItems = new List<UIHitDamageIndicatorItem>();

		private List<UIHitDamageIndicatorItem> _activeCritItems   = new List<UIHitDamageIndicatorItem>();
		private List<UIHitDamageIndicatorItem> _inactiveCritItems = new List<UIHitDamageIndicatorItem>();

		private List<UIHitDamageIndicatorItem> _activeFatalItems   = new List<UIHitDamageIndicatorItem>();
		private List<UIHitDamageIndicatorItem> _inactiveFatalItems = new List<UIHitDamageIndicatorItem>();

		private RectTransform _canvasRectTransform;
		private Canvas        _canvas;

		private List<HitData> _pendingHits = new List<HitData>(16);

		// PUBLIC METHODS

		public void HitPerformed(HitData hitData)
		{
			for (int i = 0; i < _pendingHits.Count; i++)
			{
				var pending = _pendingHits[i];

				// Try to merge hit data
				if (pending.Target == hitData.Target && pending.Target != null)
				{
					pending.Amount     += hitData.Amount;
					pending.IsFatal    |= hitData.IsFatal;
					pending.IsCritical |= hitData.IsCritical;

					_pendingHits[i] = pending;
					return;
				}
			}

			_pendingHits.Add(hitData);
		}

		// UIWidget INTERFACE

		protected override void OnInitialize()
		{
			_hitItem.SetActive(false);
			_critItem.SetActive(false);
			_fatalItem.SetActive(false);

			_canvas = GetComponentInParent<Canvas>();
			_canvasRectTransform = _canvas.transform as RectTransform;
		}

		protected override void OnHidden()
		{
			_pendingHits.Clear();
		}

		// MONOBEHAVIOUR

		private void Update()
		{
			for (int i = 0; i < _pendingHits.Count; i++)
			{
				ProcessHit(_pendingHits[i]);
			}

			_pendingHits.Clear();
		}

		private void LateUpdate()
		{
			UpdateActiveItems(_activeItems,      _inactiveItems);
			UpdateActiveItems(_activeCritItems,  _inactiveCritItems);
			UpdateActiveItems(_activeFatalItems, _inactiveFatalItems);
		}

		// PRIVATE METHODS

		private void ProcessHit(HitData hitData)
		{
			var activeItems   = hitData.IsFatal == true ? _activeFatalItems   : (hitData.IsCritical == true ? _activeCritItems   : _activeItems);
			var inactiveItems = hitData.IsFatal == true ? _inactiveFatalItems : (hitData.IsCritical == true ? _inactiveCritItems : _inactiveItems);

			var hitItem = inactiveItems.PopLast();
			if (hitItem == null)
			{
				hitItem = Instantiate(hitData.IsFatal == true ? _fatalItem : (hitData.IsCritical == true ? _critItem : _hitItem));
				hitItem.transform.SetParent(_hitItem.transform.parent);
			}

			activeItems.Add(hitItem);

			var hitPosition = hitData.Position;
			if (hitData.Target != null)
			{
				hitPosition = hitData.Target.HitPivot.position;
			}

			hitItem.Activate(hitData.Amount, hitPosition);
			hitItem.SetActive(true);
			hitItem.transform.SetAsLastSibling();
		}

		private void UpdateActiveItems(List<UIHitDamageIndicatorItem> activeItems, List<UIHitDamageIndicatorItem> inactiveItems)
		{
			for (int i = activeItems.Count; i --> 0;)
			{
				var item = activeItems[i];
				if (item.IsFinished == true)
				{
					item.SetActive(false);
					activeItems.RemoveBySwap(i);
					inactiveItems.Add(item);
					continue;
				}

				item.transform.position = GetUIPosition(item.WorldPosition);
			}
		}

		private Vector3 GetUIPosition(Vector3 worldPosition)
		{
			var screenPoint = Context.Camera.Camera.WorldToScreenPoint(worldPosition);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRectTransform, screenPoint, _canvas.worldCamera, out Vector2 screenPosition);
			return _canvasRectTransform.TransformPoint(screenPosition);
		}
	}
}
