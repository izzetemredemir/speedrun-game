using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Collections;

namespace TPSBR.UI
{
	public interface IFeedData
	{
	}

	public struct KillFeedData : IFeedData
	{
		public string    Killer;
		public string    Victim;
		public bool      IsHeadshot;
		public EHitType  DamageType;
		public bool      KillerIsLocal;
		public bool      VictimIsLocal;
	}

	public struct JoinedLeftFeedData : IFeedData
	{
		public string Nickname;
		public bool   Joined;
	}

	public struct EliminationFeedData : IFeedData
	{
		public string Nickname;
		public bool   IsLocal;
	}
	
	public struct AnnouncementFeedData : IFeedData
	{
		public string Announcement;
		public Color  Color;
	}

	public class UIKillFeed : UIWidget
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private float _minVisibilityTime;
		[SerializeField]
		private float _maxVisibilityTime;
		[SerializeField]
		private float _moveTime = 0.3f;

		private UIKillFeedItem[] _items;
		private Vector3[]        _originalPositions;
		private int              _maxFeeds;
		private Coroutine        _moveRoutine;

		private List<UIKillFeedItem> _itemsPool    = new List<UIKillFeedItem>(16);
		private List<UIKillFeedItem> _visibleFeeds = new List<UIKillFeedItem>(16);
		private List<IFeedData>      _pendingFeeds = new List<IFeedData>(16);

		// PUBLIC METHODS

		public void ShowFeed(IFeedData data)
		{
			_pendingFeeds.Add(data);
		}

		public void HideAll()
		{
			for (int i = 0; i < _maxFeeds; i++)
			{
				_items[i].SetActive(false);
			}

			_visibleFeeds.Clear();
			_pendingFeeds.Clear();

			_itemsPool.AddRange(_items);

			_moveRoutine = null;
		}

		// UIWidget

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_items = GetComponentsInChildren<UIKillFeedItem>();
			_maxFeeds = _items.Length;

			_itemsPool = new List<UIKillFeedItem>(_items);

			_originalPositions = new Vector3[_maxFeeds];

			for (int i = 0; i < _maxFeeds; i++)
			{
				var position = _items[i].transform.position;

				position.x /= Screen.width;
				position.y /= Screen.height;

				_originalPositions[i] = position;
			}
		}

		protected override void OnVisible()
		{
			base.OnVisible();

			HideAll();
		}
		protected override void OnTick()
		{
			base.OnTick();

			if (_moveRoutine != null)
				return; // Do not add or remove feeds when moving

			int visibleFeeds = _visibleFeeds.Count;

			if (_pendingFeeds.Count > 0)
			{
				if (visibleFeeds == _maxFeeds)
				{
					if (_visibleFeeds[0].VisibilityTime < _minVisibilityTime)
						return;

					HideFeedItem(0);
					return;
				}

				ShowFeedItem(_pendingFeeds[0]);
				_pendingFeeds.RemoveAt(0);
				return;
			}

			if (visibleFeeds > 0)
			{
				if (_visibleFeeds[0].VisibilityTime >= _maxVisibilityTime)
				{
					HideFeedItem(0);
				}
			}
		}

		// PRIVATE METHODS

		private void ShowFeedItem(IFeedData data)
		{
			int poolIndex = _itemsPool.Count - 1;

			var item = _itemsPool[poolIndex];
			_itemsPool.RemoveAt(poolIndex);

			_visibleFeeds.Add(item);

			item.SetData(data);
			item.RectTransform.position = GetPosition(_visibleFeeds.Count - 1);
			item.SetActive(true);
		}

		private void HideFeedItem(int index)
		{
			var feedItem = _visibleFeeds[index];

			feedItem.SetActive(false);
			_visibleFeeds.RemoveAt(index);

			_itemsPool.Add(feedItem);

			if (_visibleFeeds.Count > 0)
			{
				_moveRoutine = StartCoroutine(MoveFeeds_Coroutine());
			}
		}

		private IEnumerator MoveFeeds_Coroutine()
		{
			for (int i = 0; i < _visibleFeeds.Count; i++)
			{
				var feedItem = _visibleFeeds[i];

				DOTween.Kill(feedItem);
				_visibleFeeds[i].RectTransform.DOMove(GetPosition(i), _moveTime);
			}

			yield return new WaitForSeconds(_moveTime);

			_moveRoutine = null;
		}

		private Vector3 GetPosition(int index)
		{
			var position = _originalPositions[index];

			position.x *= Screen.width;
			position.y *= Screen.height;

			return position;
		}
	}
}
