using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace TPSBR.UI
{
	public class UISlider : Slider
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private TextMeshProUGUI  _valueText;
		[SerializeField]
		private string           _valueFormat = "f1";
		[SerializeField]
		private bool             _playValueChangedSound = true;
		[SerializeField]
		private AudioSetup       _customValueChangedSound;

		private UIWidget         _parent;

		// PUBLIC METHODS

		public void SetValue(float value)
		{
			SetValueWithoutNotify(value);
			UpdateValueText();
		}

		// MONOBEHAVIOR

		protected override void Awake()
		{
			base.Awake();

			onValueChanged.AddListener(OnValueChanged);
		}

		protected override void OnDestroy()
		{
			onValueChanged.RemoveListener(OnValueChanged);

			base.OnDestroy();
		}

		// Slider INTERFACE

		public override void OnPointerDown(PointerEventData eventData)
		{
			if (IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left)
			{
				PlayValueChangedSound();
			}

			base.OnPointerDown(eventData);
		}

		public override void OnPointerUp(PointerEventData eventData)
		{
			if (eventData.button == PointerEventData.InputButton.Left)
			{
				PlayValueChangedSound();
			}

			base.OnPointerUp(eventData);
		}

		// PRIVATE METHODS

		private void OnValueChanged(float value)
		{
			UpdateValueText();
		}

		private void UpdateValueText()
		{
			if (_valueText == null)
				return;

			_valueText.text = value.ToString(_valueFormat);
		}

		private void PlayValueChangedSound()
		{
			if (_playValueChangedSound == false)
				return;

			if (_parent == null)
			{
				_parent = GetComponentInParent<UIWidget>();
			}

			if (_parent == null)
				return;

			if (_customValueChangedSound.Clips.Length > 0)
			{
				_parent.PlaySound(_customValueChangedSound);
			}
			else
			{
				_parent.PlayClickSound();
			}
		}
	}
}
