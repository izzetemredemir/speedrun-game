using UnityEngine;

namespace TPSBR.UI
{
	public class UIDedicatedServerView : UIView 
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private UIToggle _renderToggle;

		// UIView INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_renderToggle.SetIsOnWithoutNotify(Context.Camera.EnableCamera);

			_renderToggle.onValueChanged.AddListener(OnRenderToggleValueChanged);
		}

		protected override void OnDeinitialize()
		{
			_renderToggle.onValueChanged.RemoveListener(OnRenderToggleValueChanged);

			base.OnDeinitialize();
		}

		// PRIVATE METHODS

		private void OnRenderToggleValueChanged(bool value)
		{
			Context.Camera.EnableCamera = value;
		}
	}
}
