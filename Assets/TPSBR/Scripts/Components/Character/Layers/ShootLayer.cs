using Fusion.Animations;

namespace TPSBR
{
	using UnityEngine;

	public sealed class ShootLayer : AnimationLayer
	{
		// PUBLIC MEMBERS

		public ShootState Shoot => _shoot;

		// PRIVATE MEMBERS

		[SerializeField]
		private ShootState _shoot;

		// AnimationLayer INTERFACE

		protected override void OnFixedUpdate()
		{
			if (_shoot.IsFinished() == true)
			{
				_shoot.Deactivate(0.1f);
			}
		}
	}
}
