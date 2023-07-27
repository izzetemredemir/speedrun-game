using Fusion;
using UnityEngine;

namespace TPSBR
{
	public class Target : NetworkBehaviour, IHitTarget
	{
		public Color ActiveColor = Color.green;
		public Color HitColor = Color.green;
		public Color InactiveColor = Color.gray;

		public float TimeToActive = 3;

		public float Speed;
		public float Range;

		[SerializeField]
		private Transform _hitIndicatorPivot;
		[SerializeField]
		private float _maxHealth = 100f;

		[Networked]
		public TickTimer CooldownActive { get; set; }
		[Networked]
		public TickTimer CooldownInnactive { get; set; }

		[Networked]
		public float Velocity { get; set; }
		[Networked]
		public float Health { get; set; }

		private Renderer[] _renderers;
		private Vector3 _originalPosition;

		public override void Spawned()
		{
			_renderers = GetComponentsInChildren<Renderer>();

			SetColor(ActiveColor);
			Velocity = Speed;
			Health = _maxHealth;

			_originalPosition = transform.position;
		}

		public override void FixedUpdateNetwork()
		{
			if (CooldownActive.ExpiredOrNotRunning(Runner))
			{
				SetColor(ActiveColor);
			}
			else if (CooldownInnactive.ExpiredOrNotRunning(Runner))
			{
				SetColor(InactiveColor);
				Health = _maxHealth;
			}
			else
			{
				SetColor(HitColor);
			}

			if (Object.HasStateAuthority)
			{

				var position = transform.position;
				position.z += Velocity * Runner.DeltaTime;
				var distance = position.z - _originalPosition.z;
				if (Mathf.Abs(distance) > Range)
				{
					position.z = Mathf.Clamp(position.z, _originalPosition.z - Range, _originalPosition.z + Range);
					Velocity = -Velocity;
				}

				transform.position = position;
			}

		}

		Transform IHitTarget.HitPivot => _hitIndicatorPivot != null ? _hitIndicatorPivot : transform;

		void IHitTarget.ProcessHit(ref HitData hitData)
		{
			if (CooldownActive.ExpiredOrNotRunning(Runner) == false)
			{
				hitData.Amount = 0;
				return;
			}

			float damage = Mathf.Min(hitData.Amount, Health);
			Health -= damage;

			hitData.Amount = damage;

			if (Health <= 0)
			{
				Health = 0;
				hitData.IsFatal = true;

				CooldownActive = TickTimer.CreateFromSeconds(Runner, 1 + TimeToActive);
				CooldownInnactive = TickTimer.CreateFromSeconds(Runner, 1);
			}
		}

		private void SetColor(Color color)
		{
			foreach (var r in _renderers)
			{
				r.material.color = color;
			}
		}

	}
}
