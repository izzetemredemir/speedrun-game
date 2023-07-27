using Fusion.Animations;

namespace TPSBR
{
	using UnityEngine;

	public sealed class LocomotionLayer : AnimationLayer
	{
		// PUBLIC MEMBERS

		public MoveState Move => _move;

		// PRIVATE MEMBERS

		[SerializeField]
		private MoveState _move;
	}
}
