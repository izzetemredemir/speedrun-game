using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
	public static partial class RaycastUtility
	{
		// PRIVATE MEMBERS

		private static Collider[]   _colliders   = new Collider[64];
		private static RaycastHit[] _raycastHits = new RaycastHit[64];

		// PUBLIC METHODS

		public static int ConeCast(Vector3 origin, Quaternion rotation, float angle, float maxDistance, int targetLayerMask, int blockingLayerMask, RaycastHit[] hits, Func<Collider, bool> filter = null)
		{
			if (hits == null || hits.Length == 0)
				return 0;

			Vector3 forward = rotation * new Vector3(0.0f, 0.0f, maxDistance);
			Vector3 center  = origin + forward * 0.5f;
			Vector3 corner  = Quaternion.Euler(0.0f, angle, 0.0f) * new Vector3(0.0f, 0.0f, maxDistance);
			Vector3 extents = new Vector3(Mathf.Abs(corner.x), Mathf.Abs(corner.x), maxDistance * 0.5f);

			int hitCount = 0;

			int overlapHitCount = Physics.OverlapBoxNonAlloc(center, extents, _colliders, rotation, targetLayerMask, QueryTriggerInteraction.Ignore);
			for (int i = 0; i < overlapHitCount; ++i)
			{
				Collider collider  = _colliders[i];
				Vector3  direction = collider.transform.position - origin;

				if (Vector3.Angle(forward, direction) > angle)
					continue;
				if (filter != null && filter(collider) == false)
					continue;

				int raycastHitCount = Physics.RaycastNonAlloc(origin, direction, _raycastHits, direction.magnitude, blockingLayerMask, QueryTriggerInteraction.Ignore);
				if (raycastHitCount > 0)
				{
					Sort(_raycastHits, raycastHitCount);

					RaycastHit hit = _raycastHits[0];
					if (((1 << hit.collider.gameObject.layer) & targetLayerMask) != 0)
					{
						hits[hitCount] = hit;
						++hitCount;

						if (hitCount >= hits.Length)
							break;
					}
				}
			}

			for (int i = 0; i <   _colliders.Length; ++i) {   _colliders[i] = default; }
			for (int i = 0; i < _raycastHits.Length; ++i) { _raycastHits[i] = default; }

			return hitCount;
		}

		public static void Sort(RaycastHit[] hits, int maxHits)
		{
			while (true)
			{
				bool swap = false;

				for (int i = 0; i < maxHits; ++i)
				{
					for (int j = i + 1; j < maxHits; ++j)
					{
						if (hits[j].distance < hits[i].distance)
						{
							RaycastHit hit = hits[i];
							hits[i] = hits[j];
							hits[j] = hit;

							swap = true;
						}
					}
				}

				if (swap == false)
					return;
			}
		}
	}
}
