namespace TPSBR
{
	using UnityEngine;

	public class SceneMap : SceneService
	{
		// PUBLIC MEMBERS

		public RenderTexture MapTexture      => _mapTexture;
		public Vector2Int    WorldDimensions => _worldDimensions;

		// PRIVATE MEMBERS

		[SerializeField]
		private Vector2Int    _worldDimensions;
		[SerializeField]
		private int           _prefferedResolution = 1024;
		[SerializeField]
		private Camera        _camera;

		private RenderTexture _mapTexture;

		// PUBLIC METHODS

		public void OverrideParameters(Vector3 center, Vector2Int worldDimensions)
		{
			center.y = 0f;

			transform.position = center;
			_worldDimensions = worldDimensions;

			Regenerate();
		}

		// SceneService INTERFACE

		protected override void OnInitialize()
		{
			_camera.enabled = false;
		}

		protected override void OnDeactivate()
		{
			if (_mapTexture != null)
			{
				Destroy(_mapTexture);
			}
		}

		protected override void OnActivate()
		{
			Regenerate();
		}

		// MonoBehaviour INTERFACE

		private void OnDrawGizmosSelected()
		{
			var tmpColor = Gizmos.color;
			Gizmos.color = Color.blue;

			Gizmos.DrawWireCube(transform.position, new Vector3(_worldDimensions.x, 100f, _worldDimensions.y));

			Gizmos.color = tmpColor;
		}

		// PRIVATE MEMBERS

		private void Regenerate()
		{
			if (_worldDimensions == Vector2Int.zero)
				return;

			int resolutionPerMeter = Mathf.Max(1, Mathf.RoundToInt(_prefferedResolution / (float)_worldDimensions.x));

			_mapTexture = new RenderTexture(_worldDimensions.x * resolutionPerMeter, _worldDimensions.y * resolutionPerMeter, 8, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
			_mapTexture.name = "MapTexture";
			_camera.targetTexture = _mapTexture;
			_camera.orthographicSize = Mathf.Max(_worldDimensions.x, _worldDimensions.y) / 2f;

			if (Application.isBatchMode == false)
			{
				bool fogEnabled = RenderSettings.fog;
				RenderSettings.fog = false;

				_camera.Render();

				RenderSettings.fog = fogEnabled;
			}
		}
	}
}
