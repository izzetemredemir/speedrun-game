namespace Fusion.KCC
{
	using System;
	using UnityEngine;

	public sealed class KCCRaycastHit
	{
		// PUBLIC MEMBERS

		public EColliderType Type;
		public Collider      Collider;
		public Transform     Transform;
		public bool          IsTrigger;
		public bool          IsPrimitive;
		public RaycastHit    RaycastHit;

		// PRIVATE MEMBERS

		private static readonly Type SphereColliderType  = typeof(SphereCollider);
		private static readonly Type CapsuleColliderType = typeof(CapsuleCollider);
		private static readonly Type BoxColliderType     = typeof(BoxCollider);
		private static readonly Type MeshColliderType    = typeof(MeshCollider);
#if !KCC_DISABLE_TERRAIN
		private static readonly Type TerrainColliderType = typeof(TerrainCollider);
#endif

		// PUBLIC METHODS

		public bool IsValid() => Type != EColliderType.None;

		public bool Set(RaycastHit raycastHit)
		{
			Collider collider     = raycastHit.collider;
			Type     colliderType = collider.GetType();

			if (colliderType == BoxColliderType)
			{
				Type        = EColliderType.Box;
				IsPrimitive = true;
			}
			else if (colliderType == MeshColliderType)
			{
				Type        = EColliderType.Mesh;
				IsPrimitive = false;
			}
#if !KCC_DISABLE_TERRAIN
			else if (colliderType == TerrainColliderType)
			{
				Type        = EColliderType.Terrain;
				IsPrimitive = false;
			}
#endif
			else if (colliderType == SphereColliderType)
			{
				Type        = EColliderType.Sphere;
				IsPrimitive = true;
			}
			else if (colliderType == CapsuleColliderType)
			{
				Type        = EColliderType.Capsule;
				IsPrimitive = true;
			}
			else
			{
				return false;
			}

			Collider   = collider;
			Transform  = collider.transform;
			IsTrigger  = collider.isTrigger;
			RaycastHit = raycastHit;

			return true;
		}

		public void Reset()
		{
			Type       = EColliderType.None;
			Collider   = default;
			Transform  = default;
			RaycastHit = default;
		}

		public void CopyFromOther(KCCRaycastHit other)
		{
			Type        = other.Type;
			Collider    = other.Collider;
			Transform   = other.Transform;
			IsTrigger   = other.IsTrigger;
			IsPrimitive = other.IsPrimitive;
			RaycastHit  = other.RaycastHit;
		}
	}
}
