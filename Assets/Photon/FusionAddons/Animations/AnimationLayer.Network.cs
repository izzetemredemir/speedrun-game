namespace Fusion.Animations
{
	using System.Collections.Generic;

	public abstract unsafe partial class AnimationLayer
	{
		// PUBLIC METHODS

		public void InitializeNetworkProperties(AnimationController controller, List<AnimationNetworkProperty> networkProperties)
		{
			AnimationState[] states = _states;
			for (int i = 0, count = states.Length; i < count; ++i)
			{
				AnimationState state = states[i];
				state.InitializeNetworkProperties(controller, networkProperties);
			}

			OnInitializeNetworkProperties(controller, networkProperties);
		}

		// AnimationLayer INTERFACE

		protected virtual void OnInitializeNetworkProperties(AnimationController controller, List<AnimationNetworkProperty> networkProperties) {}
	}
}
