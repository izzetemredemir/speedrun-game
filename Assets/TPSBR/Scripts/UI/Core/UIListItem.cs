using System;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
	public sealed class UIListItem : UIListItemBase<MonoBehaviour>
	{
	}

	public abstract class UIListItemBase<T> : UIBehaviour where T : MonoBehaviour
	{
		// PUBLIC MEMBERS

		public int  ID { get; set; }
		public T    Content => _content;
		public bool IsSelectable => _button != null;
		public bool IsSelected { get { return _isSelected; } set { SetIsSelected(value); } }
		public bool IsInteractable { get { return GetIsInteractable(); } set { SetIsInteractable(value); } }

		public Action<int> Clicked;

		// PRIVATE MEMBERS

		[SerializeField]
		private Button _button;
		[SerializeField]
		private Animator _animator;
		[SerializeField]
		private T _content;
		[SerializeField]
		private string _selectedAnimatorParameter = "IsSelected";
		[SerializeField]
		private CanvasGroup _selectedGroup;
		[SerializeField]
		private CanvasGroup _deselectedGroup;

		private bool _isSelected;

		// MONOBEHAVIOR

		protected virtual void Awake()
		{
			SetIsSelected(false, true);

			if (_button != null)
			{
				_button.onClick.AddListener(OnClick);
			}

			if (_button != null && _button.transition == Selectable.Transition.Animation)
			{
				_animator = _button.animator;
			}
		}

		protected virtual void OnDestroy()
		{
			Clicked = null;

			if (_button != null)
			{
				_button.onClick.RemoveListener(OnClick);
			}
		}

		// PRIVATE METHODS

		private void SetIsSelected(bool value, bool force = false)
		{
			if (_isSelected == value && force == false)
				return;

			_isSelected = value;

			_selectedGroup.SetVisibility(value);
			_deselectedGroup.SetVisibility(value == false);

			UpdateAnimator();
		}

		private bool GetIsInteractable()
		{
			return _button != null ? _button.interactable : false;
		}

		private void SetIsInteractable(bool value)
		{
			if (_button == null)
				return;

			_button.interactable = value;
		}

		private void OnClick()
		{
			Clicked?.Invoke(ID);
		}

		private void UpdateAnimator()
		{
			if (_animator == null)
				return;

			if (_selectedAnimatorParameter.HasValue() == false)
				return;

			if (_animator != null)
			{
				_animator.SetBool(_selectedAnimatorParameter, _isSelected);
			}
		}
	}
}
