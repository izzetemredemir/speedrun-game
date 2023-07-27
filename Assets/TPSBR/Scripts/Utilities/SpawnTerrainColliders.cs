namespace Fusion.KCC
{
	using UnityEngine;

	[RequireComponent(typeof(TerrainCollider))]
	public sealed class SpawnTerrainColliders : MonoBehaviour
	{
		[SerializeField]
		private string _treeTag = "Wood";

		private void Awake()
		{
			Transform       transform       = this.transform;
			TerrainCollider terrainCollider = GetComponent<TerrainCollider>();
			TerrainData     terrainData     = terrainCollider.terrainData;
			TreeInstance[]  treeInstances   = terrainData.treeInstances;
			TreePrototype[] treePrototypes  = terrainData.treePrototypes;
			Collider[]      treeColliders   = new Collider[treePrototypes.Length];
			Vector3[]       treeOffsets     = new Vector3[treePrototypes.Length];
			int[]           treeLayers      = new int[treePrototypes.Length];

			Vector3 baseSize     = terrainData.size;
			Vector3 basePosition = transform.position;

			for (int i = 0, count = treePrototypes.Length; i < count; ++i)
			{
				TreePrototype treePrototype = treePrototypes[i];
				if (treePrototype.prefab != null)
				{
					Collider treeCollider = treePrototype.prefab.GetComponent<Collider>();
					if (treeCollider != null)
					{
						treeColliders[i] = treeCollider;
						treeLayers[i]    = treePrototype.prefab.layer;
					}
					else
					{
						treeCollider = treePrototype.prefab.GetComponentInChildren<Collider>();
						if (treeCollider != null)
						{
							treeColliders[i] = treeCollider;
							treeOffsets[i]   = treeCollider.transform.localPosition - treePrototype.prefab.transform.position;
							treeLayers[i]    = treeCollider.gameObject.layer;
						}
					}
				}
			}

			Vector3 position;

			for (int i = 0, count = treeInstances.Length; i < count; ++i)
			{
				TreeInstance treeInstance = treeInstances[i];
				Collider     treeCollider = treeColliders[treeInstance.prototypeIndex];

				if (treeCollider == null)
					continue;

				position = treeInstance.position;
				position.x *= baseSize.x;
				position.y *= baseSize.y;
				position.z *= baseSize.z;

				position += basePosition;

				if (treeInstance.rotation.IsAlmostZero() == false && treeOffsets[treeInstance.prototypeIndex].IsAlmostZero() == false)
				{
					position += Quaternion.Euler(0.0f, treeInstance.rotation * Mathf.Rad2Deg, 0.0f) * treeOffsets[treeInstance.prototypeIndex];
				}

				GameObject colliderGO = new GameObject("TreeCollider");
				colliderGO.layer = treeLayers[treeInstance.prototypeIndex];
				colliderGO.tag = _treeTag;
				colliderGO.transform.SetParent(transform, false);
				colliderGO.transform.position = position;

				float widthScale = treeInstance.widthScale;

				if (treeCollider is BoxCollider treeBoxCollider)
				{
					BoxCollider boxCollider = colliderGO.AddComponent<BoxCollider>();
					boxCollider.material = treeBoxCollider.material;
					boxCollider.center   = treeBoxCollider.center;
					boxCollider.size     = treeBoxCollider.size.OnlyXZ() * widthScale + treeBoxCollider.size.OnlyY();

					if (treeInstance.rotation.IsAlmostZero() == false)
					{
						boxCollider.center = Quaternion.Euler(0.0f, treeInstance.rotation * Mathf.Rad2Deg, 0.0f) * boxCollider.center;
					}
				}
				else if (treeCollider is CapsuleCollider treeCapsuleCollider)
				{
					CapsuleCollider capsuleCollider = colliderGO.AddComponent<CapsuleCollider>();
					capsuleCollider.direction = treeCapsuleCollider.direction;
					capsuleCollider.material  = treeCapsuleCollider.material;
					capsuleCollider.center    = treeCapsuleCollider.center;
					capsuleCollider.height    = treeCapsuleCollider.height;
					capsuleCollider.radius    = treeCapsuleCollider.radius * widthScale;

					if (treeInstance.rotation.IsAlmostZero() == false)
					{
						capsuleCollider.center = Quaternion.Euler(0.0f, treeInstance.rotation * Mathf.Rad2Deg, 0.0f) * capsuleCollider.center;
					}
				}
				else if (treeCollider is MeshCollider treeMeshCollider)
				{
					MeshCollider meshCollider = colliderGO.AddComponent<MeshCollider>();
					meshCollider.material   = treeMeshCollider.material;
					meshCollider.sharedMesh = treeMeshCollider.sharedMesh;
					meshCollider.convex     = treeMeshCollider.convex;
				}
				else if (treeCollider is SphereCollider treeSphereCollider)
				{
					SphereCollider sphereCollider = colliderGO.AddComponent<SphereCollider>();
					sphereCollider.material = treeSphereCollider.material;
					sphereCollider.center   = treeSphereCollider.center;
					sphereCollider.radius   = treeSphereCollider.radius * widthScale;

					if (treeInstance.rotation.IsAlmostZero() == false)
					{
						sphereCollider.center = Quaternion.Euler(0.0f, treeInstance.rotation * Mathf.Rad2Deg, 0.0f) * sphereCollider.center;
					}
				}
			}
		}
	}
}
