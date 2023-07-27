using UnityEngine;

namespace TPSBR
{
	public class FuelPickup : StaticPickup
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private int _fuel = 200;

		// StaticPickup INTERFACE

		protected override bool Consume(Agent agent, out string result)
		{
			bool fuelAdded = agent.Jetpack.AddFuel(_fuel);
			result = fuelAdded == true ? string.Empty : "Cannot add more fuel";

			return fuelAdded;
		}
	}
}
