using UnityEngine;

namespace TPSBR
{
	public class RuntimeSettings
	{
		// CONSTANTS

		public const string KEY_MUSIC_VOLUME     = "MusicVolume";
		public const string KEY_EFFECTS_VOLUME   = "EffectsVolume";
		public const string KEY_WINDOWED         = "Windowed";
		public const string KEY_RESOLUTION       = "Resolution";
		public const string KEY_GRAPHICS_QUALITY = "GraphicsQuality";
		public const string KEY_LIMIT_FPS        = "LimitFPS";
		public const string KEY_TARGET_FPS       = "TargetFPS";
		public const string KEY_REGION           = "Region";
		public const string KEY_SENSITIVITY      = "Sensitivity";
		public const string KEY_AIM_SENSITIVITY  = "AimSensitivity";
		public const string KEY_VSYNC            = "VSync";

		// PUBLIC MEMBERS

		public Options Options => _options;

		public float  MusicVolume    { get { return _options.GetFloat(KEY_MUSIC_VOLUME); }     set { _options.Set(KEY_MUSIC_VOLUME, value, false); } }
		public float  EffectsVolume  { get { return _options.GetFloat(KEY_EFFECTS_VOLUME); }   set { _options.Set(KEY_EFFECTS_VOLUME, value, false); } }

		public bool   Windowed        { get { return _options.GetBool(KEY_WINDOWED); }         set { _options.Set(KEY_WINDOWED, value, false); } }
		public int    Resolution      { get { return _options.GetInt(KEY_RESOLUTION); }        set { _options.Set(KEY_RESOLUTION, value, false); } }
		public int    GraphicsQuality { get { return _options.GetInt(KEY_GRAPHICS_QUALITY); }  set { _options.Set(KEY_GRAPHICS_QUALITY, value, false); } }
		public bool   VSync           { get { return _options.GetBool(KEY_VSYNC); }            set { _options.Set(KEY_VSYNC, value, false); } }
		public bool   LimitFPS        { get { return _options.GetBool(KEY_LIMIT_FPS); }        set { _options.Set(KEY_LIMIT_FPS, value, false); } }
		public int    TargetFPS       { get { return _options.GetInt(KEY_TARGET_FPS); }        set { _options.Set(KEY_TARGET_FPS, value, false); } }
		public float  Sensitivity     { get { return _options.GetFloat(KEY_SENSITIVITY); }     set { _options.Set(KEY_SENSITIVITY, value, false); } }
		public float  AimSensitivity  { get { return _options.GetFloat(KEY_AIM_SENSITIVITY); } set { _options.Set(KEY_AIM_SENSITIVITY, value, false); } }

		public string Region          { get { return _options.GetString(KEY_REGION); }         set { _options.Set(KEY_REGION, value, true); } }


		// PRIVATE MEMBERS

		private Options _options = new Options();

		// PUBLIC METHODS

		public void Initialize(GlobalSettings settings)
		{
			_options.Initialize(settings.DefaultOptions, true, "Options.V3.");

			Windowed = Screen.fullScreen == false;
			GraphicsQuality = QualitySettings.GetQualityLevel();
			Resolution = GetCurrentResolutionIndex();

			QualitySettings.vSyncCount = VSync == true ? 1 : 0;
			Application.targetFrameRate = LimitFPS == true ? TargetFPS : -1;

			_options.SaveChanges();
		}

		// PRIVATE MEMBERS

		private int GetCurrentResolutionIndex()
		{
			var resolutions = Screen.resolutions;
			if (resolutions == null || resolutions.Length == 0)
				return -1;

			int currentWidth = Mathf.RoundToInt(Screen.width);
			int currentHeight = Mathf.RoundToInt(Screen.height);
			int defaultRefreshRate = resolutions[^1].refreshRate;

			for (int i = 0; i < resolutions.Length; i++)
			{
				var resolution = resolutions[i];

				if (resolution.width == currentWidth && resolution.height == currentHeight && resolution.refreshRate == defaultRefreshRate)
					return i;
			}

			return -1;
		}
	}
}
