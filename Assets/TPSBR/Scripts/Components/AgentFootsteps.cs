using Fusion;
using UnityEngine;

namespace TPSBR
{
	[OrderAfter(typeof(LateAgentController))]
	public sealed class AgentFootsteps : SimulationBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private AudioEffect _leftFootEffect;
		[SerializeField]
		private AudioEffect _rightFootEffect;
		[SerializeField]
		private FootstepSetup _footstepSetup;
		[SerializeField]
		private float _defaultDelay = 0.03f;
		[SerializeField]
		private float _minFootstepTime = 0.2f;
		[SerializeField]
		private float _checkDistanceUp = 0.1f;
		[SerializeField]
		private float _checkDistanceDown = 0.05f;
		[SerializeField]
		private float _maxStepCorrectionTime = 0.7f;
		[SerializeField]
		private float _minSpeed = 0.1f;
		[SerializeField]
		private float _runSpeedTreshold = 2f;
		[SerializeField]
		private LayerMask _hitMask;

		private FootData _leftFootData = new FootData();
		private FootData _rightFootData = new FootData();

		private float _lastStepTime;

		private Character _character;

		// MONOBEHAVIOUR

		private void Awake()
		{
			_character = GetComponent<Character>();

			_leftFootData.Transform = _character.ThirdPersonView.LeftFoot;
			_rightFootData.Transform = _character.ThirdPersonView.RightFoot;

			_leftFootData.Effect = _leftFootEffect;
			_rightFootData.Effect = _rightFootEffect;
		}

		// SimulationBehaviour INTERFACE

		public override void Render()
		{
			PlayFootsteps();
		}

		// PRIVATE METHODS

		private void PlayFootsteps()
		{
			_leftFootData.Cooldown -= Time.deltaTime;
			_rightFootData.Cooldown -= Time.deltaTime;

			if (_character.CharacterController.FixedData.IsGrounded == false)
			{
				_leftFootData.IsUp = true;
				_rightFootData.IsUp = true;
				return;
			}

			if (_character.CharacterController.FixedData.RealSpeed < _minSpeed)
				return;

			float timeFromLastStep = Time.time - _lastStepTime;

			CheckFoot(_leftFootData, timeFromLastStep);
			CheckFoot(_rightFootData, timeFromLastStep);
		}

		private void CheckFoot(FootData foot, float timeFromAnyLastStep)
		{
			if (foot.IsUp == true && foot.Cooldown > 0f)
				return;

			float distanceFromBottom = (foot.Transform.position - transform.position).y;

			if (foot.IsUp == false && distanceFromBottom > _checkDistanceUp)
			{
				foot.IsUp = true;
			}
			else if (foot.IsUp == true && distanceFromBottom < _checkDistanceDown)
			{
				var newSetupSource = _footstepSetup.GetSound(GetSurfaceTagHash(foot.Transform), _character.CharacterController.FixedData.RealSpeed > _runSpeedTreshold);

				if (foot.SetupSource != newSetupSource)
				{
					foot.Setup.CopyFrom(newSetupSource);
					foot.SetupSource = newSetupSource;
				}

				float timeFromThisLastStep = -foot.Cooldown + _minFootstepTime;

				if (timeFromAnyLastStep < _maxStepCorrectionTime && timeFromThisLastStep * 0.5f < _maxStepCorrectionTime)
				{
					// Pace correction
					foot.Setup.Delay = (timeFromThisLastStep * 0.5f - timeFromAnyLastStep) * 0.75f;
				}
				else
				{
					foot.Setup.Delay = _defaultDelay;
				}

				foot.Effect.Play(foot.Setup, EForceBehaviour.ForceAny);

				// Make sure same sound is not played twice in a row
				_leftFootData.Effect.LastPlayedClipIndex = foot.Effect.LastPlayedClipIndex;
				_rightFootData.Effect.LastPlayedClipIndex = foot.Effect.LastPlayedClipIndex;

				_lastStepTime = Time.time + foot.Setup.Delay;

				foot.IsUp = false;
				foot.Cooldown = _minFootstepTime;
			}
		}

		private int GetSurfaceTagHash(Transform foot)
		{
			var physicsScene = Runner.SimulationUnityScene.GetPhysicsScene();
			if (physicsScene.Raycast(foot.position + Vector3.up, Vector3.down, out RaycastHit hit, 1.5f, _hitMask, QueryTriggerInteraction.Collide) == true)
			{
				var collider = hit.collider;
				if (collider != null)
				{
					return collider.tag.GetHashCode();
				}
			}

			return 0;
		}

		// HELPERS

		private class FootData
		{
			public bool        IsUp;
			public float       Cooldown;
			public Transform   Transform;
			public AudioEffect Effect;
			public AudioSetup  Setup = new AudioSetup();
			public AudioSetup  SetupSource;
		}
	}
}
