namespace Fusion.KCC
{
	using UnityEngine;

	public sealed class KCCRaycastInfo
	{
		// PUBLIC MEMBERS

		public Vector3                 Origin;
		public Vector3                 Direction;
		public float                   MaxDistance;
		public float                   Radius;
		public LayerMask               LayerMask;
		public QueryTriggerInteraction TriggerInteraction;
		public KCCRaycastHit[]         AllHits;
		public int                     AllHitCount;
		public KCCRaycastHit[]         ColliderHits;
		public int                     ColliderHitCount;
		public KCCRaycastHit[]         TriggerHits;
		public int                     TriggerHitCount;

		// CONSTRUCTORS

		public KCCRaycastInfo(int maxHits)
		{
			AllHits      = new KCCRaycastHit[maxHits];
			TriggerHits  = new KCCRaycastHit[maxHits];
			ColliderHits = new KCCRaycastHit[maxHits];

			for (int i = 0; i < maxHits; ++i)
			{
				AllHits[i] = new KCCRaycastHit();
			}
		}

		// PUBLIC METHODS

		public void AddHit(RaycastHit raycastHit)
		{
			if (AllHitCount == AllHits.Length)
				return;

			KCCRaycastHit hit = AllHits[AllHitCount];
			if (hit.Set(raycastHit) == true)
			{
				++AllHitCount;

				if (hit.IsTrigger == true)
				{
					TriggerHits[TriggerHitCount] = hit;
					++TriggerHitCount;
				}
				else
				{
					ColliderHits[ColliderHitCount] = hit;
					++ColliderHitCount;
				}
			}
		}

		public void Reset(bool deep)
		{
			Origin             = default;
			Direction          = default;
			MaxDistance        = default;
			Radius             = default;
			LayerMask          = default;
			TriggerInteraction = QueryTriggerInteraction.Collide;
			AllHitCount        = default;
			ColliderHitCount   = default;
			TriggerHitCount    = default;

			if (deep == true)
			{
				for (int i = 0, count = AllHits.Length; i < count; ++i)
				{
					AllHits[i].Reset();
				}
			}
		}
	}
}
