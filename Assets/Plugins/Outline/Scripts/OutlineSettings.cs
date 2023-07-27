namespace Plugins.Outline
{
	using System;
	using UnityEngine;
	using UnityEngine.Rendering.Universal;

	public enum EOutlineUpdateMode
	{
		None     = 0,
		Self     = 1,
		Children = 2,
	}

	[Serializable]
	public class OutlineSettings : IOutlineSettings
	{
		public const int MinWidth     = 0;
		public const int MaxWidth     = 32;
		public const int MinIntensity = 0;
		public const int MaxIntensity = 100;

		[SerializeField]
		private OutlineSettingsObject _settings;
		[SerializeField, Range(MinWidth, MaxWidth)]
		private float _width = 1.0f;
		[SerializeField, Range(MinIntensity, MaxIntensity)]
		private float _intensity = 1.0f;
		[SerializeField]
		private Color _color = Color.white;
		[SerializeField]
		private RenderPassEvent _pass = RenderPassEvent.AfterRenderingTransparents;
		[SerializeField]
		private EOutlineUpdateMode _updateMode = EOutlineUpdateMode.None;

		public float              Width      { get { return _settings != null ? _settings.Width     : _width;     } set { _width     = Mathf.Clamp(value, MinWidth, MaxWidth);         } }
		public float              Intensity  { get { return _settings != null ? _settings.Intensity : _intensity; } set { _intensity = Mathf.Clamp(value, MinIntensity, MaxIntensity); } }
		public Color              Color      { get { return _settings != null ? _settings.Color     : _color;     } set { _color     = value;  } }
		public RenderPassEvent    Pass       { get { return _settings != null ? _settings.Pass      : _pass;      } set { _pass      = value;  } }
		public EOutlineUpdateMode UpdateMode { get { return _updateMode; } set { _updateMode = value;  } }
	}
}
