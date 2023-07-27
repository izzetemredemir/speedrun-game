namespace TPSBR
{
	using UnityEngine;
	using Fusion.KCC;

	public sealed class MoveSpeedKCCProcessor : KCCProcessor
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private float _moveSpeedMultiplier = 1.0f;

		// KCCProcessor INTERFACE

		public override float Priority => float.MinValue;

		public override EKCCStages GetValidStages(KCC kcc, KCCData data)
		{
			return EKCCStages.SetKinematicSpeed;
		}

		public override void SetKinematicSpeed(KCC kcc, KCCData data)
		{
			data.KinematicSpeed *= _moveSpeedMultiplier;
		}
	}
}
