using UnityEngine;
using Fusion;

namespace TPSBR
{
	public class Flashbang : ContextBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private float      _innerRadius = 10f;
		[SerializeField]
		private float      _outerRadius = 25f;

		[SerializeField]
		private float      _innerFlashValue = 1f;
		[SerializeField]
		private float      _outerFlashValue = 0.1f;
		[SerializeField]
		private float      _maxFlashDuration = 4f;
		[SerializeField]
		private float      _minFlashDuration = 0.5f;
		[SerializeField]
		private float      _flashFalloffDelayMultiplier = 0.8f;
		[SerializeField]
		private float      _maxFlashDot = 0.7f;
		[SerializeField]
		private float      _minFlashDot = -0.5f;
		[SerializeField]
		private float      _minDotMultiplier = 0.3f;
		[SerializeField]
		private bool       _affectsOwner = true;

		[SerializeField]
		private float      _despawnDelay = 3f;

		[SerializeField]
		private GameObject _effectRoot;

		private TickTimer  _despawnTimer;

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			base.Spawned();

			ShowEffect();
			Explode();
		}

		public override void FixedUpdateNetwork()
		{
			if (Object.HasStateAuthority == false)
				return;
			if (_despawnTimer.Expired(Runner) == false)
				return;

			Runner.Despawn(Object);
		}

		// MONOBEHAVIOUR

		protected void OnEnable()
		{
			_effectRoot.SetActive(false);
		}

		// PRIVATE METHODS

		private void Explode()
		{
			if (Object.HasStateAuthority == false)
				return;

			var position     = transform.position + Vector3.up * 0.5f; // Take position slightly above
			var flashFalloff = _innerRadius < _outerRadius && _innerFlashValue != _outerFlashValue;

			foreach (var player in Context.NetworkGame.Players)
			{
				if (player == null)
					continue;

				if (_affectsOwner == false && player.Object.InputAuthority == Object.InputAuthority)
					continue;

				var agent = player.ActiveAgent;
				if (agent == null || agent.Health.IsAlive == false)
					continue;

				var agentHead = agent.transform.position + Vector3.up * 1.8f;
				var directionToAgent = agentHead - position;

				if (directionToAgent.sqrMagnitude > _outerRadius * _outerRadius)
					continue;

				float distance = directionToAgent.magnitude;
				directionToAgent /= distance; // Normalize

				if (Context.Runner.GetPhysicsScene().Raycast(position, directionToAgent, distance, ObjectLayerMask.Default) == true)
					continue;

				float flash = _innerFlashValue;
				float duration = _maxFlashDuration;

				if (flashFalloff == true && distance > _innerRadius)
				{
					float progress = (_outerRadius - _innerRadius) / (distance - _innerRadius);

					flash = Mathf.Lerp(_innerFlashValue, _outerFlashValue, progress);
					duration = Mathf.Lerp(_maxFlashDuration, _minFlashDuration, progress);
				}

				if (_minDotMultiplier < 1f)
				{
					float dot = Vector3.Dot(agent.Character.CharacterController.Data.LookDirection, -directionToAgent);
					float dotMultiplier = MathUtility.Map(_minFlashDot, _maxFlashDot, _minDotMultiplier, 1f, dot);

					flash *= dotMultiplier;
					duration *= dotMultiplier;
				}

				agent.Senses.SetEyesFlash(flash, duration, duration * _flashFalloffDelayMultiplier);
			}

			_despawnTimer = TickTimer.CreateFromSeconds(Runner, _despawnDelay);
		}

		private void ShowEffect()
		{
			if (Runner.Mode == SimulationModes.Server)
				return;

			_effectRoot.SetActive(true);
		}
	}
}
