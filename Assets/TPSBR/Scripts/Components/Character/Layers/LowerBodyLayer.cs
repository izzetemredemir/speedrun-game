namespace TPSBR
{
	using UnityEngine;
	using Fusion.KCC;
	using Fusion.Animations;

	public sealed class LowerBodyLayer : AnimationLayer
	{
		// PUBLIC MEMBERS

		public TurnState Turn => _turn;

		// PRIVATE MEMBERS

		[SerializeField]
		private TurnState _turn;

		private KCC _kcc;

		// AnimationLayer INTERFACE

		protected override void OnInitialize()
		{
			_kcc = Controller.GetComponent<KCC>();
		}

		protected override void OnFixedUpdate()
		{
			float blendTime = 0.2f;

			if (_kcc.FixedData.RealSpeed < 0.1f && _turn.RemainingTime.IsAlmostZero(blendTime * _turn.BlendSpeed * _turn.TurnSpeed) == false)
			{
				_turn.Activate(blendTime);
			}
			else
			{
				_turn.Deactivate(blendTime * 0.5f);
			}
		}
	}
}
