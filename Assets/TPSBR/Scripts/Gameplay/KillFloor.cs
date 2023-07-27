using UnityEngine;

namespace TPSBR
{
	public class KillFloor : ContextBehaviour
	{
		// NetworkBehaviour INTERFACE

		public override void FixedUpdateNetwork()
		{
			if (Object.HasStateAuthority == false)
				return;

			if (Context == null || Context.NetworkGame.Object == null)
				return;

			float yPosition = transform.position.y;

			foreach (var player in Context.NetworkGame.Players)
			{
				if (player == null)
					continue;

				var agent = player.ActiveAgent;
				if (agent == null)
					continue;

				if (agent.Health.IsAlive == false)
					continue;

				if (agent.transform.position.y > yPosition)
					continue;

				var hitData = new HitData()
				{
					Action    = EHitAction.Damage,
					HitType   = EHitType.Suicide,
					Amount    = 99999f,
					IsFatal   = true,
					Position  = agent.transform.position,
					Target    = agent.Health,
					Normal    = Vector3.up,
				};

				HitUtility.ProcessHit(ref hitData);
			}
		}
	}
}
