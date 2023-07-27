namespace Plugins.Outline
{
	using UnityEngine;
	using UnityEngine.Rendering.Universal;

	[CreateAssetMenu(fileName = "OutlineSettings", menuName = "Outline/Settings")]
	public sealed class OutlineSettingsObject : ScriptableObject, IOutlineSettings
	{
		public const float MinWidth     = OutlineSettings.MinWidth;
		public const float MaxWidth     = OutlineSettings.MaxWidth;
		public const float MinIntensity = OutlineSettings.MinIntensity;
		public const float MaxIntensity = OutlineSettings.MaxIntensity;

		[SerializeField, Range(OutlineSettings.MinWidth, OutlineSettings.MaxWidth)]
		private float _width = 1.0f;
		[SerializeField, Range(OutlineSettings.MinIntensity, OutlineSettings.MaxIntensity)]
		private float _intensity = 1.0f;
		[SerializeField]
		private Color _color = Color.white;
		[SerializeField]
		private RenderPassEvent _pass = RenderPassEvent.AfterRenderingTransparents;

		public float              Width      { get { return _width;     } set { _width     = Mathf.Clamp(value, MinWidth, MaxWidth);         } }
		public float              Intensity  { get { return _intensity; } set { _intensity = Mathf.Clamp(value, MinIntensity, MaxIntensity); } }
		public Color              Color      { get { return _color;     } set { _color     = value;  } }
		public RenderPassEvent    Pass       { get { return _pass;      } set { _pass      = value;  } }
		public EOutlineUpdateMode UpdateMode { get { return EOutlineUpdateMode.None; } set {} }
	}
}
