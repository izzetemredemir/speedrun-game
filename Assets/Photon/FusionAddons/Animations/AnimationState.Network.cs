namespace Fusion.Animations
{
	using System.Collections.Generic;

	public abstract unsafe partial class AnimationState
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

		// AnimationState INTERFACE

		protected virtual void OnInitializeNetworkProperties(AnimationController controller, List<AnimationNetworkProperty> networkProperties) {}
	}
}
