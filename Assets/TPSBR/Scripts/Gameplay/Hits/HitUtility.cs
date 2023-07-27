using Fusion;
using UnityEngine;

namespace TPSBR
{
	public enum EHitAction
	{
		None,
		Damage,
		Heal,
		Shield,
	}

	public struct HitData
	{
		public EHitAction     Action;
		public float          Amount;
		public bool           IsCritical;
		public bool           IsFatal;
		public Vector3        Position;
		public Vector3        Normal;
		public Vector3        Direction;
		public PlayerRef      InstigatorRef;
		public IHitInstigator Instigator;
		public IHitTarget     Target;
		public EHitType       HitType;
	}

	public enum EHitType
	{
		None,
		Pistol,
		Rifle,
		Grenade,
		Suicide,
		Heal,
		SMG,
		Shotgun,
		Sniper,
		ShrinkingArea,
	}

	public interface IHitTarget
	{
		Transform HitPivot { get; }
		void ProcessHit(ref HitData hit);
	}

	public interface IHitInstigator
	{
		void HitPerformed(HitData hit);
	}

	public static class HitUtility
	{
		// PUBLIC METHODS

		public static bool ProcessHit(PlayerRef instigatorRef, Vector3 direction, LagCompensatedHit hit, float baseDamage, EHitType hitType, out HitData processedHit)
		{
			processedHit = default;

			IHitTarget target = hit.Hitbox != null ? hit.Hitbox.Root.GetComponent<IHitTarget>() : null;
			if (target == null)
				return false;

			processedHit.Action        = EHitAction.Damage;
			processedHit.Amount        = baseDamage;
			processedHit.Position      = hit.Point;
			processedHit.Normal        = hit.Normal;
			processedHit.Direction     = direction;
			processedHit.Target        = target;
			processedHit.InstigatorRef = instigatorRef;
			processedHit.HitType       = hitType;

			if (hit.Hitbox is BodyPart bodyPart)
			{
				processedHit.Amount = baseDamage * bodyPart.DamageMultiplier;
				processedHit.IsCritical = bodyPart.IsCritical;
			}

			return ProcessHit(ref processedHit);
		}

		public static bool ProcessHit(NetworkBehaviour instigator, Vector3 direction, LagCompensatedHit hit, float baseDamage, EHitType hitType, out HitData processedHit)
		{
			processedHit = default;

			IHitTarget target = hit.Hitbox != null ? hit.Hitbox.Root.GetComponent<IHitTarget>() : null;
			if (target == null)
				return false;

			if (hit.Hitbox.Root.gameObject == instigator)
				return false;

			processedHit.Action        = EHitAction.Damage;
			processedHit.Amount        = baseDamage;
			processedHit.Position      = hit.Point;
			processedHit.Normal        = hit.Normal;
			processedHit.Direction     = direction;
			processedHit.Target        = target;
			processedHit.InstigatorRef = instigator != null ? instigator.Object.InputAuthority : default;
			processedHit.Instigator    = instigator != null ? instigator.GetComponent<IHitInstigator>() : null;
			processedHit.HitType       = hitType;

			if (hit.Hitbox is BodyPart bodyPart)
			{
				processedHit.Amount = baseDamage * bodyPart.DamageMultiplier;
				processedHit.IsCritical = bodyPart.IsCritical;
			}

			return ProcessHit(ref processedHit);
		}

		public static bool ProcessHit(NetworkBehaviour instigator, Collider collider, float damage, EHitType hitType, out HitData processedHit)
		{
			processedHit = new HitData();

			var target = collider.GetComponentInParent<IHitTarget>();
			if (target == null)
				return false;

			processedHit.Action        = EHitAction.Damage;
			processedHit.Amount        = damage;
			processedHit.InstigatorRef = instigator.Object.InputAuthority;
			processedHit.Instigator    = instigator.GetComponent<IHitInstigator>();
			processedHit.Position      = collider.transform.position;
			processedHit.Normal        = (instigator.transform.position - collider.transform.position).normalized;
			processedHit.Direction     = -processedHit.Normal;
			processedHit.Target        = target;
			processedHit.HitType       = hitType;

			return ProcessHit(ref processedHit);
		}

		public static bool ProcessHit(ref HitData hitData)
		{
			hitData.Target.ProcessHit(ref hitData);

			// For local debug targets we show hit feedback immediately
			if (hitData.Instigator != null && hitData.Target is Health == false)
			{
				hitData.Instigator.HitPerformed(hitData);
			}

			return true;
		}
	}
}
