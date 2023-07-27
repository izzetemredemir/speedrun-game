using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TPSBR.UI
{
	public class UIValue : UIBehaviour
	{
		// PUBLIC MEMBERS

		public bool                ShowPercentage  { get { return _showPercentage; } set { _showPercentage = value; _formatInitialized = false; } }
		public int                 DecimalNumbers  { get { return _decimalNumbers; } set { _decimalNumbers = value; _formatInitialized = false; } }
		public bool                ShowMaximum     { get { return _showMaximum; }    set { _showMaximum    = value; _formatInitialized = false; } }
		public float               Value           { get { return _value; } }
		public float               MaxValue        { get { return _maxValue; } }
		public new TextMeshProUGUI Text            { get { return _text; } }
		public Image               Fill            { get { return _fill; } }

		// PRIVATE MEMBERS

		[SerializeField]
		private Image           _fill;
		[SerializeField]
		private TextMeshProUGUI _text;
		[SerializeField]
		private bool            _showPercentage;
		[SerializeField]
		private bool            _showMaximum;
		[SerializeField]
		private int             _decimalNumbers;
		[SerializeField]
		[Tooltip("Example: \"Available in {0} seconds\"")]
		private string          _textFormat;
		[SerializeField]
		private string          _infinitySymbol = "~";
		[SerializeField]
		private float           _minChange = 0.01f;

		[Header("Time Setup")]
		[SerializeField]
		private bool            _displayInTimeFormat;
		[SerializeField]
		private bool            _showZeroHours;
		[SerializeField]
		private bool            _showZeroMinutes;
		[SerializeField]
		private bool            _showZeroSeconds;

		private float           _value = float.MinValue;
		private float           _maxValue;

		private bool            _formatInitialized;
		private string          _format;

		// PUBLIC METHODS

		public void SetValue(float value, float maxValue = 0.0f)
		{
			if (Mathf.Abs(_value - value) < _minChange == true &&  Mathf.Abs(_maxValue - maxValue) < _minChange == true)
				return;

			if (_formatInitialized == false)
			{
				InitializeFormat();
			}

			_value    = value;
			_maxValue = maxValue;

			if (_text != null)
			{
				if (_displayInTimeFormat == true)
				{
					int hours = (int)(value / 3600);
					int minutes = (int)(value / 60) - hours * 60;
					int seconds = (int)(value % 60);

					string timeString = string.Empty;

					if (hours > 0 || _showZeroHours == true)
					{
						timeString = $"{hours}:{minutes:00}:{seconds:00}";
					}
					else if (minutes > 0 || _showZeroMinutes == true)
					{
						timeString = $"{minutes}:{seconds:00}";
					}
					else if (seconds > 0 || _showZeroSeconds == true)
					{
						timeString = $"{seconds}";
					}

					_text.text = string.Format(_format, timeString);
				}
				else
				{
					float textValue = _showPercentage == true ? _value / _maxValue : _value;

					if (textValue < float.MaxValue && maxValue < float.MaxValue)
					{
						_text.text = _showMaximum == true ? string.Format(_format, textValue, maxValue) : string.Format(_format, textValue);
					}
					else
					{
						string stringValue    = textValue < float.MaxValue ? textValue.ToString() : _infinitySymbol;
						string stringMaxValue = maxValue < float.MaxValue ? maxValue.ToString() : _infinitySymbol;

						_text.text = _showMaximum == true ? string.Format(_format, stringValue, stringMaxValue) : string.Format(_format, stringValue);
					}
				}
			}

			if (_fill != null)
			{
				if (_fill.type == Image.Type.Filled)
				{
					_fill.fillAmount = _value / (_maxValue == 0.0f ? 1.0f : _maxValue);
				}
				else
				{
					_fill.rectTransform.anchorMax = new Vector2(_value / _maxValue, _fill.rectTransform.anchorMax.y);
				}
			}
		}

		public void SetFillColor(Color color)
		{
			_fill.color = color;
		}

		// PRIVATE METHODS

		private void InitializeFormat()
		{
			if (_text == null)
				return;

			if (_displayInTimeFormat == true)
			{
				_format = $"{{0}}";
			}
			else
			{
				string numberFormat = _showPercentage == true ? "P" + _decimalNumbers : "N" + _decimalNumbers;

				if (_showMaximum == true)
				{
					_format = $"{{0:{numberFormat}}} / {{1:{numberFormat}}}";
				}
				else
				{
					_format = $"{{0:{numberFormat}}}";
				}
			}

			_format = _textFormat.HasValue() == true ? string.Format(_textFormat, _format) : _format;
			_formatInitialized = true;
		}
	}
}
