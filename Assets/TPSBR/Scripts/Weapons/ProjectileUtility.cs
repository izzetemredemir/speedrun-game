using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
	public static class ProjectileUtility
	{
		public static bool ProjectileCast(NetworkRunner runner, PlayerRef owner, int ownerObjectInstanceID, Vector3 firePosition, Vector3 direction, float raycastDistance, LayerMask hitMask, List<LagCompensatedHit> validHits)
		{
			return ProjectileCast(runner, owner, ownerObjectInstanceID, firePosition, direction, 0f, raycastDistance, hitMask, validHits);
		}

		public static bool ProjectileCast(NetworkRunner runner, PlayerRef owner, int ownerObjectInstanceID, Vector3 firePosition, Vector3 direction, float distanceToTarget, float raycastDistance, LayerMask hitMask, List<LagCompensatedHit> validHits)
		{
			validHits.Clear();

			var hits = ListPool.Get<LagCompensatedHit>(16);
			int hitCount = runner.LagCompensation.RaycastAll(firePosition, direction, raycastDistance, owner, hits, hitMask, true, HitOptions.IncludePhysX | HitOptions.SubtickAccuracy);

			if (hitCount <= 0)
			{
				ListPool.Return(hits);
				return false;
			}

			var hitRoots = ListPool.Get<int>(16);

			float ignoreDistanceMin = 3f;
			float ignoreDistanceMax = Mathf.Max(distanceToTarget - 1f, ignoreDistanceMin);

			Sort(hits, hitCount);

			for (int i = 0; i < hits.Count; i++)
			{
				var hit = hits[i];

				if (distanceToTarget > 0f)
				{
					// For 3rd person cast it is possible that we want to shoot through environment objects a little bit if we already know the destination point
					// => ray from camera is different than ray from 3rd person character
					if (hit.Distance >= ignoreDistanceMin && hit.Distance < ignoreDistanceMax && hit.GameObject != null && ObjectLayerMask.Environment.value.IsBitSet(hit.GameObject.layer))
						continue;
				}

				int hitRootID = hit.Hitbox != null ? hit.Hitbox.Root.gameObject.GetInstanceID() : 0;

				if (hitRootID != 0)
				{
					if (hitRootID == ownerObjectInstanceID)
						continue; // Owner was hit

					if (hitRoots.Contains(hitRootID) == true)
						continue; // Same object was hit multiple times

					hitRoots.Add(hitRootID);
				}

				validHits.Add(hits[i]);
			}

			ListPool.Return(hitRoots);
			ListPool.Return(hits);

			return validHits.Count > 0;
		}

		public static void Sort(List<LagCompensatedHit> hits, int maxHits)
		{
			while (true)
			{
				bool swap = false;

				for (int i = 0; i < maxHits; ++i)
				{
					for (int j = i + 1; j < maxHits; ++j)
					{
						if (hits[j].Distance < hits[i].Distance)
						{
							LagCompensatedHit hit = hits[i];
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
