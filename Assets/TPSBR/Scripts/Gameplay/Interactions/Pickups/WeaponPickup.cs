using UnityEngine;

namespace TPSBR
{
	public class WeaponPickup : StaticPickup
	{
		// PUBLIC MEMBERS

		public Weapon WeaponPrefab => _weaponPrefab;

		// PRIVATE MEMBERS

		[SerializeField]
		private Weapon _weaponPrefab;

		// StaticPickup INTERFACE

		protected override bool Consume(Agent agent, out string result)
		{
			result = string.Empty;
			return true;
		}

		protected override string InteractionName        => (_weaponPrefab as IDynamicPickupProvider).Name;
		protected override string InteractionDescription => (_weaponPrefab as IDynamicPickupProvider).Description;
	}
}
