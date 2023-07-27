using UnityEngine;
using Fusion.KCC;

namespace TPSBR
{
	using Fusion.Animations;

	public sealed class FullBodyLayer : AnimationLayer
	{
		// PUBLIC MEMBERS

		public JumpState    Jump    => _jump;
		public FallState    Fall    => _fall;
		public LandState    Land    => _land;
		public DeadState    Dead    => _dead;
		public JetpackState Jetpack => _jetpack;

		// PRIVATE MEMBERS

		[SerializeField]
		private JumpState    _jump;
		[SerializeField]
		private FallState    _fall;
		[SerializeField]
		private LandState    _land;
		[SerializeField]
		private DeadState    _dead;
		[SerializeField]
		private JetpackState _jetpack;

		private KCC _kcc;

		// AnimationState INTERFACE

		protected override void OnInitialize()
		{
			_kcc = Controller.GetComponentNoAlloc<KCC>();
		}

		protected override void OnFixedUpdate()
		{
			KCCData kccData = _kcc.FixedData;

			if (kccData.HasJumped == true)
			{
				_jump.Activate(0.2f);
				return;
			}

			AnimationState activeState = GetActiveState();
			if (activeState == null)
			{
				if (kccData.IsGrounded == false && kccData.WasGrounded == false)
				{
					_fall.Activate(0.2f);
				}

				return;
			}

			if (activeState == _jump)
			{
				if (kccData.IsGrounded == true)
				{
					if (kccData.InputDirection.IsAlmostZero(0.1f) == true)
					{
						_land.Activate(0.2f);
					}
					else
					{
						_jump.Deactivate(0.1f);
					}
				}
				else if (_jump.IsFinished(1.0f) == true)
				{
					_fall.Activate(0.2f);
				}
			}
			else if (activeState == _fall)
			{
				if (kccData.IsGrounded == true)
				{
					if (kccData.InputDirection.IsAlmostZero(0.1f) == true)
					{
						_land.Activate(0.1f);
					}
					else
					{
						_fall.Deactivate(0.1f);
					}
				}
			}
			else if (activeState == _land)
			{
				if (kccData.IsGrounded == true)
				{
					if (kccData.InputDirection.IsAlmostZero(0.1f) == false)
					{
						_land.Deactivate(0.1f);
					}
				}
				if (_land.IsFinished(1.0f) == true)
				{
					_land.Deactivate(0.1f);
				}
			}
		}
	}
}
