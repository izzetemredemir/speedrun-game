namespace TPSBR
{
	using UnityEngine;
	using Fusion;
	using Fusion.KCC;

	[DisallowMultipleComponent]
	public sealed unsafe class StaticNetworkTransform : NetworkAreaOfInterestBehaviour
	{
		// PRIVATE MEMBERS

		private static float _positionReadAccuracy  = float.NaN;
		private static float _positionWriteAccuracy = float.NaN;

		// NetworkAreaOfInterestBehaviour INTERFACE

		public override int PositionWordOffset => 0;

		// NetworkBehaviour INTERFACE

		public override int? DynamicWordCount => 6;

		public override void Spawned()
		{
			if (_positionReadAccuracy.IsNaN() == true)
			{
				_positionReadAccuracy  = new Accuracy(AccuracyDefaults.POSITION).Value;
				_positionWriteAccuracy = _positionReadAccuracy > 0.0f ? 1.0f / _positionReadAccuracy : 0.0f;
			}

			if (Object.HasStateAuthority == true)
			{
				KCCNetworkUtility.WriteVector3(Ptr, transform.position, _positionWriteAccuracy);
				KCCNetworkUtility.WriteVector3(Ptr + 3, transform.rotation.eulerAngles, 0.0f);
			}
			else
			{
				transform.position = KCCNetworkUtility.ReadVector3(Ptr, _positionReadAccuracy);
				transform.rotation = Quaternion.Euler(KCCNetworkUtility.ReadVector3(Ptr + 3, 0.0f));
			}
		}
	}
}
