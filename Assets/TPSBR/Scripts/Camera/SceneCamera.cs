using UnityEngine;

namespace TPSBR
{
	public class SceneCamera : SceneService
	{
		// PUBLIC MEMBERS

		public Camera      Camera        => _camera;
		public ShakeEffect ShakeEffect   => _shakeEffect;
		public bool        EnableCamera  { get; set; } = true;

		// PRIVATE MEMBERS

		[SerializeField]
		private Camera _camera;
		[SerializeField]
		private AudioListener _audioListener;
		[SerializeField]
		private ShakeEffect _shakeEffect;

		private int _cameraCullingMask;

		// SceneService INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_cameraCullingMask = _camera.cullingMask;
		}

		protected override void OnTick()
		{
			if (Scene is Gameplay)
			{
				_audioListener.enabled = Context.HasInput;
				_camera.enabled = Context.HasInput;

				// We are just switching culling mask as disabling would mean more complex camera setup to not stop UI rendering
				_camera.cullingMask = EnableCamera == true ? _cameraCullingMask : 0;
			}
		}
	}
}
