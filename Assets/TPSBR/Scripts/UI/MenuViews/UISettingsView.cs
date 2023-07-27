using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TPSBR.UI
{
	public class UISettingsView : UICloseView
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private UISlider _musicVolumeSlider;
		[SerializeField]
		private UISlider _effectsVolumeSlider;

		[SerializeField]
		private UISlider _sensitivitySlider;
		[SerializeField]
		private UISlider _aimSensitivitySlider;

		[SerializeField]
		private TMP_Dropdown _graphicsQuality;
		[SerializeField]
		private TMP_Dropdown _resolution;
		[SerializeField]
		private UIToggle _vSync;
		[SerializeField]
		private UIToggle _limitFPS;
		[SerializeField]
		private UISlider _targetFPS;
		[SerializeField]
		private int[] _targetFPSValues;
		[SerializeField]
		private UIToggle _windowed;

		[SerializeField]
		private UIButton _confirmButton;
		[SerializeField]
		private UIButton _resetButton;

		private List<ResolutionData> _validResolutions = new List<ResolutionData>(32);

		// UICloseView INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_confirmButton.onClick.AddListener(OnConfirmButton);
			_resetButton.onClick.AddListener(OnResetButton);

			_musicVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
			_effectsVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);

			_sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
			_aimSensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);

			_graphicsQuality.onValueChanged.AddListener(OnGraphicsChanged);
			_resolution.onValueChanged.AddListener(OnGraphicsChanged);
			_targetFPS.onValueChanged.AddListener(OnTargetFPSChanged);
			_limitFPS.onValueChanged.AddListener(OnLimitFPSChanged);
			_vSync.onValueChanged.AddListener(OnLimitFPSChanged);

			_windowed.onValueChanged.AddListener(OnWindowedChanged);
		}

		protected override void OnDeinitialize()
		{
			_confirmButton.onClick.RemoveListener(OnConfirmButton);
			_resetButton.onClick.RemoveListener(OnResetButton);

			_musicVolumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
			_effectsVolumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);

			_sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
			_aimSensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);

			_graphicsQuality.onValueChanged.RemoveListener(OnGraphicsChanged);
			_resolution.onValueChanged.RemoveListener(OnGraphicsChanged);
			_targetFPS.onValueChanged.RemoveListener(OnTargetFPSChanged);
			_limitFPS.onValueChanged.RemoveListener(OnLimitFPSChanged);
			_vSync.onValueChanged.RemoveListener(OnLimitFPSChanged);

			_windowed.onValueChanged.RemoveListener(OnWindowedChanged);

			base.OnDeinitialize();
		}

		protected override void OnOpen()
		{
			base.OnOpen();

			PrepareResolutionDropdown();

			LoadValues();
		}

		protected override void OnClose()
		{
			base.OnClose();

			Context.RuntimeSettings.Options.DiscardChanges();

			Context.Audio.UpdateVolume();
		}

		protected override void OnTick()
		{
			base.OnTick();

			_confirmButton.interactable = Context.RuntimeSettings.Options.HasUnsavedChanges;

			_limitFPS.SetActive(_vSync.isOn == false);
			_targetFPS.SetActive(_vSync.isOn == false && _limitFPS.isOn);
		}

		// PRIVATE METHODS

		private void LoadValues()
		{
			var runtimeSettings = Context.RuntimeSettings;

			_musicVolumeSlider.SetOptionsValueFloat(runtimeSettings.Options.GetValue(RuntimeSettings.KEY_MUSIC_VOLUME));
			_effectsVolumeSlider.SetOptionsValueFloat(runtimeSettings.Options.GetValue(RuntimeSettings.KEY_EFFECTS_VOLUME));

			_sensitivitySlider.SetOptionsValueFloat(runtimeSettings.Options.GetValue(RuntimeSettings.KEY_SENSITIVITY));
			_aimSensitivitySlider.SetOptionsValueFloat(runtimeSettings.Options.GetValue(RuntimeSettings.KEY_AIM_SENSITIVITY));

			_windowed.SetIsOnWithoutNotify(runtimeSettings.Windowed);
			_graphicsQuality.SetValueWithoutNotify(runtimeSettings.GraphicsQuality);
			_resolution.SetValueWithoutNotify(_validResolutions.FindIndex(t => t.Index == runtimeSettings.Resolution));
			_targetFPS.SetOptionsValueInt(runtimeSettings.Options.GetValue(RuntimeSettings.KEY_TARGET_FPS));
			_limitFPS.SetIsOnWithoutNotify(runtimeSettings.LimitFPS);
			_vSync.SetIsOnWithoutNotify(runtimeSettings.VSync);
		}

		private void OnConfirmButton()
		{
			Context.RuntimeSettings.Options.SaveChanges();

			var runtimeSettings = Context.RuntimeSettings;

			if (Application.isMobilePlatform == false || Application.isEditor == true)
			{
				var resolution = Screen.resolutions[runtimeSettings.Resolution < 0 ? Screen.resolutions.Length - 1 : runtimeSettings.Resolution];
				Screen.SetResolution(resolution.width, resolution.height, _windowed.isOn == false);
			}

			QualitySettings.SetQualityLevel(runtimeSettings.GraphicsQuality);

			Application.targetFrameRate = runtimeSettings.LimitFPS == true ? runtimeSettings.TargetFPS : -1;
			QualitySettings.vSyncCount = runtimeSettings.VSync == true ? 1 : 0;

			Close();
		}

		private void OnResetButton()
		{
			var options = Context.RuntimeSettings.Options;

			options.ResetValueToDefault(RuntimeSettings.KEY_EFFECTS_VOLUME, false);
			options.ResetValueToDefault(RuntimeSettings.KEY_MUSIC_VOLUME, false);
			options.ResetValueToDefault(RuntimeSettings.KEY_GRAPHICS_QUALITY, false);
			options.ResetValueToDefault(RuntimeSettings.KEY_RESOLUTION, false);
			options.ResetValueToDefault(RuntimeSettings.KEY_WINDOWED, false);
			options.ResetValueToDefault(RuntimeSettings.KEY_LIMIT_FPS, false);
			options.ResetValueToDefault(RuntimeSettings.KEY_TARGET_FPS, false);
			options.ResetValueToDefault(RuntimeSettings.KEY_SENSITIVITY, false);
			options.ResetValueToDefault(RuntimeSettings.KEY_AIM_SENSITIVITY, false);
			options.ResetValueToDefault(RuntimeSettings.KEY_VSYNC, false);

			LoadValues();

			Context.Audio.UpdateVolume();
		}

		private void OnVolumeChanged(float value)
		{
			Context.RuntimeSettings.MusicVolume = _musicVolumeSlider.value;
			Context.RuntimeSettings.EffectsVolume = _effectsVolumeSlider.value;

			Context.Audio.UpdateVolume();
		}

		private void OnSensitivityChanged(float value)
		{
			Context.RuntimeSettings.Sensitivity = _sensitivitySlider.value;
			Context.RuntimeSettings.AimSensitivity = _aimSensitivitySlider.value;
		}

		private void OnLimitFPSChanged(bool value)
		{
			OnGraphicsChanged(-1);
		}

		private void OnTargetFPSChanged(float value)
		{
			OnGraphicsChanged(-1);
		}

		private void OnGraphicsChanged(int value)
		{
			var runtimeSettings = Context.RuntimeSettings;

			runtimeSettings.GraphicsQuality = _graphicsQuality.value;
			runtimeSettings.Resolution = _validResolutions[_resolution.value].Index;
			runtimeSettings.TargetFPS = Mathf.RoundToInt(_targetFPS.value);
			runtimeSettings.LimitFPS = _limitFPS.isOn;
			runtimeSettings.VSync = _vSync.isOn;
		}

		private void OnWindowedChanged(bool value)
		{
			Context.RuntimeSettings.Windowed = value;
		}

		private void PrepareResolutionDropdown()
		{
			_validResolutions.Clear();
			var resolutions = Screen.resolutions;

			int defaultRefreshRate = resolutions[^1].refreshRate;

			// Add resolutions in reversed order
			for (int i = resolutions.Length - 1; i >= 0; i--)
			{
				var resolution = resolutions[i];
				if (resolution.refreshRate != defaultRefreshRate)
					continue;

				_validResolutions.Add(new ResolutionData(i, resolution));
			}


			var options = ListPool.Get<TMP_Dropdown.OptionData>(16);

			for (int i = 0; i < _validResolutions.Count; i++)
			{
				var resolution = _validResolutions[i].Resolution;
				options.Add(new TMP_Dropdown.OptionData($"{resolution.width} x {resolution.height}"));
			}

			_resolution.ClearOptions();
			_resolution.AddOptions(options);

			ListPool.Return(options);
		}

		// HELPERS

		private struct ResolutionData
		{
			public int Index;
			public Resolution Resolution;

			public ResolutionData(int index, Resolution resolution)
			{
				Index = index;
				Resolution = resolution;
			}
		}
	}

	public static class UISliderExtensions
	{
		public static void SetOptionsValueFloat(this UISlider slider, OptionsValue value)
		{
			if (value.Type != EOptionsValueType.Float)
			{
				slider.value = 0f;
				return;
			}

			if (slider.minValue != value.FloatValue.MinValue || slider.maxValue != value.FloatValue.MaxValue)
			{
				// Setting min and max value will unfortunately always trigger onValueChanged
				// so we need to removed this event data and reassign it again agterwards

				Slider.SliderEvent onValueChanged = slider.onValueChanged;
				slider.onValueChanged = new Slider.SliderEvent();

				slider.minValue = value.FloatValue.MinValue;
				slider.maxValue = value.FloatValue.MaxValue;

				slider.onValueChanged = onValueChanged;
			}

			slider.SetValue(value.FloatValue.Value);
		}

		public static void SetOptionsValueInt(this UISlider slider, OptionsValue value)
		{
			if (value.Type != EOptionsValueType.Int)
			{
				slider.value = 0f;
				return;
			}

			if (slider.minValue != value.IntValue.MinValue || slider.maxValue != value.IntValue.MaxValue)
			{
				// Setting min and max value will unfortunately always trigger onValueChanged
				// so we need to removed this event data and reassign it again agterwards

				Slider.SliderEvent onValueChanged = slider.onValueChanged;
				slider.onValueChanged = new Slider.SliderEvent();

				slider.minValue = value.IntValue.MinValue;
				slider.maxValue = value.IntValue.MaxValue;

				slider.onValueChanged = onValueChanged;
			}

			slider.SetValue(value.IntValue.Value);
		}
	}
}
