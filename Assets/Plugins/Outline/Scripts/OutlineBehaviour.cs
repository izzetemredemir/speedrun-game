namespace Plugins.Outline
{
	using UnityEngine;
	using UnityEngine.Rendering.Universal;

	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	public sealed class OutlineBehaviour : MonoBehaviour
	{
		private static RenderPassEvent InvalidPass = (RenderPassEvent)999;

		public OutlineSettings Settings = new OutlineSettings();

		private bool            _isRegistered   = false;
		private RenderPassEvent _registeredPass = InvalidPass;

		public void Refresh()
		{
			UnregisterObject();

			Refresh(enabled);
		}

		private void Update()
		{
			Refresh(enabled);
		}

		private void OnEnable()
		{
			Refresh(true);
		}

		private void OnDisable()
		{
			Refresh(false);
		}

		private void OnDestroy()
		{
			Refresh(false);
		}

		private void Refresh(bool isEnabled)
		{
			if (_isRegistered == true)
			{
				if (isEnabled == true)
				{
					if (_registeredPass == Settings.Pass)
						return;

					UnregisterObject();
					RegisterObject();
				}
				else
				{
					UnregisterObject();
				}
			}
			else
			{
				if (isEnabled == true)
				{
					RegisterObject();
				}
			}
		}

		private void RegisterObject()
		{
			if (_isRegistered == true)
				return;

			if (OutlineFeature.RegisterObject(gameObject, Settings) == false)
				return;

			_isRegistered   = true;
			_registeredPass = Settings.Pass;
		}

		private void UnregisterObject()
		{
			if (_isRegistered == false)
				return;

			OutlineFeature.UnregisterObject(gameObject);

			_isRegistered   = false;
			_registeredPass = InvalidPass;
		}
	}
}
