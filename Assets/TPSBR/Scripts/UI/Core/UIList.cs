using UnityEngine;
using System;
using System.Collections.Generic;

namespace TPSBR.UI
{
	public class UIList : UIListBase<UIListItem, MonoBehaviour>
	{
	}

	public abstract class UIListBase<TListItem, RContent> : UIBehaviour
		where TListItem: UIListItemBase<RContent>
		where RContent: MonoBehaviour
	{
		// PUBLIC MEMBERS

		public event Action<int, RContent> UpdateContent;
		public event Action<int> SelectionChanged;

		public int Selection { get { return _selection; } set { SetSelection(value, false); } }
		public int Count => _dataCount;

		// PRIVATE MEMBERS

		[SerializeField]
		private bool _allowSelection = true;
		[SerializeField]
		private bool _allowDeselection = true;
		[SerializeField]
		private TListItem _itemInstance;

		private List<TListItem> _items = new List<TListItem>(32);

		private int _dataCount;
		private int _selection = -1;

		// PUBLIC METHODS

		public void Refresh(int dataCount, bool notifySelection = true)
		{
			_dataCount = dataCount;

			UpdateItems();

			if (_selection >= _dataCount)
			{
				SetSelection(_allowDeselection == false && _dataCount > 0 ? 0 : -1, notifySelection, true);
			}
			else if(_selection < 0 && _allowDeselection == false && _dataCount > 0)
			{
				SetSelection(0, notifySelection, true);
			}
			else
			{
				SetSelection(_selection, false, true);
			}
		}

		public void Clear(bool destroyItems = true)
		{
			_dataCount = 0;

			if (destroyItems == true)
			{
				_itemInstance.SetActive(false);

				for (int i = 1; i < _items.Count; i++)
				{
					Destroy(_items[i].gameObject);
				}

				_items.Clear();
			}
			else
			{
				UpdateItems();
			}

			if (_selection >= 0)
			{
				SetSelection(-1, true);
			}
		}

		// MONOBEHAVIOR

		protected void Awake()
		{
			if (_dataCount == 0)
			{
				_itemInstance.SetActive(false);
			}
		}

		// PRIVATE METHODS

		private void SetSelection(int selection, bool notify, bool force = false)
		{
			if (_allowSelection == false)
				return;

			if (selection >= _dataCount)
				return;

			if (selection == _selection && force == false)
				return;

			if (selection < 0)
			{
				selection = -1;
			}

			_selection = selection;

			for (int i = 0; i < _dataCount; i++)
			{
				var item = _items[i];
				item.IsSelected = item.ID == _selection;
			}

			if (notify == true)
			{
				SelectionChanged?.Invoke(_selection);
			}
		}

		private void UpdateItems()
		{
			bool selectable = _itemInstance.IsSelectable;
			var parent = _itemInstance.transform.parent;

			for (int i = _items.Count; i < _dataCount; i++)
			{
				var newItem = i == 0 ? _itemInstance : Instantiate(_itemInstance, parent);

				newItem.ID = i;

				if (selectable == true)
				{
					newItem.Clicked -= OnItemClicked;
					newItem.Clicked += OnItemClicked;
				}

				_items.Add(newItem);
			}

			for (int i = 0; i < _items.Count; i++)
			{
				var item = _items[i];

				if (i < _dataCount)
				{
					UpdateContent?.Invoke(i, item.Content);
					item.SetActive(true);
				}
				else
				{
					item.SetActive(false);
				}
			}
		}

		private void OnItemClicked(int id)
		{
			if (id == _selection && _allowDeselection == false)
				return;

			SetSelection(id == _selection ? -1 : id, true);
		}
	}
}
