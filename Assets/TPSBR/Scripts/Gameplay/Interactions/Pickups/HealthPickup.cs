using UnityEngine;

namespace TPSBR
{
	public class HealthPickup : StaticPickup
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private int _amount = 10;
		[SerializeField]
		private EHitAction _actionType;

		// StaticPickup INTERFACE

		protected override bool Consume(Agent agent, out string result)
		{
			var hitData = new HitData
			{
				Action        = _actionType,
				Amount        = _amount,
				InstigatorRef = Object.InputAuthority,
				Target        = agent.Health,
				HitType       = EHitType.Heal,
			};

			HitUtility.ProcessHit(ref hitData);

			result = hitData.Amount > 0f ? string.Empty : (_actionType == EHitAction.Shield ? "Shield full" : "Health full");

			return hitData.Amount > 0f;
		}
	}
}
