using Fusion;

namespace TPSBR
{
	using System;

	[OrderAfter(typeof(Agent), typeof(HitboxManager))]
	public sealed class LateAgentController : SimulationBehaviour
	{
		private Action _fixedUpdate;
		private Action _render;

		public void SetDelegates(Action fixedUpdateDelegate, Action renderDelegate)
		{
			_fixedUpdate = fixedUpdateDelegate;
			_render      = renderDelegate;
		}

		public override void FixedUpdateNetwork()
		{
			if (_fixedUpdate != null)
			{
				_fixedUpdate();
			}
		}

		public override void Render()
		{
			if (_render != null)
			{
				_render();
			}
		}
	}
}
