using UnityEngine;
using UnityEngine.UI;

namespace RTS.UI
{
	[System.Serializable]
	class UIColoredElements
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private Graphic[] _elements;

		private Color[] _originalColors;
		private Color   _currentColor;
		private bool    _colorSet;

		// PUBLIC METHODS

		public void SetColor(Color color, bool force = false)
		{
			if (_colorSet == true && color == _currentColor && force == false)
				return;

			SetElementsColor(color);

			_currentColor = color;
			_colorSet = true;
		}

		public void ResetColor()
		{
			if (_originalColors == null)
				return;

			ResetElementsColor();
			_colorSet = false;
		}

		// PRIVATE METHODS

		private void SetElementsColor(Color color)
		{
			SaveOriginalColors();

			for (int i = 0; i < _elements.Length; i++)
			{
				var element = _elements[i];

				if (element == null)
					continue;

				element.color = color;
			}
		}

		private void ResetElementsColor()
		{
			for (int i = 0; i < _elements.Length; i++)
			{
				var element = _elements[i];

				if (element == null)
					continue;

				element.color = _originalColors[i];
			}
		}

		private void SaveOriginalColors()
		{
			if (_originalColors != null)
				return;

			_originalColors = new Color[_elements.Length];

			for (int i = 0; i < _elements.Length; i++)
			{
				var element = _elements[i];

				if (element == null)
					continue;

				_originalColors[i] = element.color;
			}
		}
	}
}
