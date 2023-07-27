using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace TPSBR.UI
{
	public class UIToggleGroup : ToggleGroup
	{
		// PUBLIC MEMBERS

		public Action<int> ValueChanged;

		// PRIVATE MEMBERS

		private List<Toggle> _toggles = new List<Toggle>(8);

		// PUBLIC METHODS

		public void SetValue(int value)
		{
			if (_toggles.Count == 0)
			{
				FindToggles();
			}

			for (int i = 0; i < _toggles.Count; i++)
			{
				_toggles[i].SetIsOnWithoutNotify(i == value);
			}
		}

		public int GetValue()
		{
			if (_toggles.Count == 0)
			{
				FindToggles();
			}

			for (int i = 0; i < _toggles.Count; i++)
			{
				if (_toggles[i].isOn == true)
					return i;
			}

			return -1;
		}

		// MONOBEHAVIOR

		protected override void Start()
		{
			base.Start();

			if (_toggles.Count == 0)
			{
				FindToggles();
			}
		}

		protected override void OnDestroy()
		{
			ClearToggles();

			ValueChanged = null;

			base.OnDestroy();
		}

		// PRIVATE METHODS

		private void OnToggleValueChanged(bool value)
		{
			if (value == false && _toggles[0].group != null && _toggles[0].group.allowSwitchOff == false)
				return;

			ValueChanged?.Invoke(GetValue());
		}

		private void FindToggles()
		{
			ClearToggles();

			GetComponentsInChildren(true, _toggles);

			for (int i = _toggles.Count - 1; i >= 0; i--)
			{
				Toggle toggle = _toggles[i];

				if (toggle.group == this)
				{
					toggle.onValueChanged.AddListener(OnToggleValueChanged);
				}
				else
				{
					_toggles.RemoveAt(i);
				}
			}
		}

		private void ClearToggles()
		{
			for (int i = 0; i < _toggles.Count; i++)
			{
				Toggle toggle = _toggles[i];

				if (toggle != null)
				{
					toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
				}
			}

			_toggles.Clear();
		}
	}
}
