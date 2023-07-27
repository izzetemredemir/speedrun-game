using UnityEngine;

namespace TPSBR
{
	public class AmmoPickup : StaticPickup
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private int _weaponSlot = 1;
		[SerializeField]
		private int _amount = 50;

		// StaticPickup INTERFACE

		protected override bool Consume(Agent agent, out string result)
		{
			return agent.Weapons.AddAmmo(_weaponSlot, _amount, out result);
		}
	}
}
