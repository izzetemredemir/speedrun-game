using Fusion.KCC;
using UnityEngine;

namespace TPSBR
{
	public sealed class JetpackKCCProcessor : BaseKCCProcessor
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private float _moveForce = 8f;
		[SerializeField]
		private float _moveDownForce = 8f;
		[SerializeField]
		private float _thrustUpForce = 20f;
		[SerializeField]
		private float _fullThrustUpForce = 30f;
		[SerializeField]
		private float _lookThrustForce = 2f;
		[SerializeField]
		private float _groundThrustImpulse = 1f;
		[SerializeField]
		private float _upThrustOppositeVelocityMultiplier = 0.2f;
		[SerializeField]
		private float _moveThrustOppositeVelocityMultiplier = 0.05f;
		[SerializeField]
		private float _gravityMultiplier = 1.5f;
		[SerializeField]
		private float _airPhysicsFriction = 0.001f;
		[SerializeField]
		private float _groundPhysicsFriction = 0.25f;
		[SerializeField]
		private float _moveVelocityDecreaseSpeed = 5f;

		private bool    _hasJetpack;
		private Jetpack _jetpack;

		// KCCProcessor INTERFACE

		public override float Priority => float.MaxValue;

		public override void OnEnter(KCC kcc, KCCData data)
		{
			_jetpack = kcc.GetComponent<Jetpack>();
			_hasJetpack = _jetpack != null;
		}

		public override EKCCStages GetValidStages(KCC kcc, KCCData data)
		{
			EKCCStages stages = EKCCStages.None;

			if (_hasJetpack == true && _jetpack.IsActive == true)
			{
				stages |= EKCCStages.SetInputProperties;
				stages |= EKCCStages.SetDynamicVelocity;
				stages |= EKCCStages.SetKinematicDirection;
				stages |= EKCCStages.SetKinematicTangent;
				stages |= EKCCStages.SetKinematicSpeed;
				stages |= EKCCStages.SetKinematicVelocity;
				stages |= EKCCStages.ProcessPhysicsQuery;
				stages |= EKCCStages.OnStay;
			}

			return stages;
		}

		public override void SetInputProperties(KCC kcc, KCCData data)
		{
			float thrustForce = 0f;

			if (_jetpack.IsRunning == true)
			{
				thrustForce = _jetpack.FullThrust == true ? _fullThrustUpForce : _thrustUpForce;
			}

			data.ExternalForce += Vector3.up * thrustForce;

			if (thrustForce > 0f && _jetpack.FullThrust == true && data.RealVelocity.y < 0f && data.RealVelocity.y < 0f)
			{
				data.ExternalVelocity += new Vector3(0f, -data.DynamicVelocity.y * _upThrustOppositeVelocityMultiplier, 0f);
			}

			if (data.IsGrounded == true && _jetpack.FullThrust == true)
			{
				data.ExternalImpulse += Vector3.up * _groundThrustImpulse;
			}

			kcc.SuppressProcessors<IKCCProcessor>();
		}

		public override void SetDynamicVelocity(KCC kcc, KCCData data)
		{
			if (data.IsGrounded == true && data.IsSnappingToGround == false && data.DynamicVelocity.y < 0.0f && data.DynamicVelocity.OnlyXZ().IsAlmostZero())
			{
				data.DynamicVelocity.y = 0f;
			}

			if (_lookThrustForce > 0f)
			{
				var lookDirection = data.LookRotation * Vector3.forward;
				data.ExternalForce += lookDirection * _lookThrustForce;
			}

			if (_moveDownForce > 0f && data.InputDirection != default)
			{
				// Player is able to fly down faster
				var rawInputDirection = Quaternion.Inverse(data.TransformRotation) * data.InputDirection;
				var desiredDirection = data.LookRotation * rawInputDirection;

				if (desiredDirection.y < 0f)
				{
					data.ExternalForce += Vector3.down * -desiredDirection.normalized.y * _moveDownForce;
				}
			}

			if (_moveForce > 0f && data.KinematicDirection != default)
			{
				var velocityXZ = data.RealVelocity.OnlyXZ();
				float oppositeDirectionDot = Vector3.Dot(-velocityXZ.normalized, data.KinematicTangent);

				if (oppositeDirectionDot > 0f)
				{
					data.ExternalVelocity += -velocityXZ * oppositeDirectionDot * _moveThrustOppositeVelocityMultiplier;
				}

				data.ExternalForce += data.KinematicTangent * _moveForce;
			}

			data.DynamicVelocity += data.Gravity * _gravityMultiplier * data.DeltaTime;

			data.DynamicVelocity += data.ExternalVelocity;
			data.DynamicVelocity += data.ExternalAcceleration * data.DeltaTime;
			data.DynamicVelocity += (data.ExternalImpulse / kcc.Settings.Mass);
			data.DynamicVelocity += (data.ExternalForce / kcc.Settings.Mass) * data.DeltaTime;

			Vector3 velocityDirection = data.KinematicTangent;
			float speed = data.DynamicVelocity.magnitude;

			if (speed > 0f)
			{
				velocityDirection = data.DynamicVelocity / speed;
			}

			data.DynamicVelocity += -0.5f * (data.IsGrounded == true ? _groundPhysicsFriction : _airPhysicsFriction) * velocityDirection * speed * speed;

			kcc.SuppressProcessors<IKCCProcessor>();
		}

		public override void SetKinematicDirection(KCC kcc, KCCData data)
		{
			data.KinematicDirection = data.InputDirection;

			kcc.SuppressProcessors<IKCCProcessor>();
		}

		public override void SetKinematicTangent(KCC kcc, KCCData data)
		{
			data.KinematicTangent = default;

			if (data.KinematicDirection != default)
			{
				data.KinematicTangent = data.KinematicDirection.normalized;
			}

			if (data.KinematicTangent == default)
			{
				data.KinematicTangent = data.TransformDirection;
			}

			kcc.SuppressProcessors<IKCCProcessor>();
		}

		public override void SetKinematicSpeed(KCC kcc, KCCData data)
		{
			kcc.SuppressProcessors<IKCCProcessor>();
		}

		public override void SetKinematicVelocity(KCC kcc, KCCData data)
		{
			// Move velocity is only decreasing (e.g. after jetpack activation)
			data.KinematicVelocity = Vector3.Lerp(data.KinematicVelocity, default, data.DeltaTime * _moveVelocityDecreaseSpeed);

			kcc.SuppressProcessors<IKCCProcessor>();
		}

		public override void ProcessPhysicsQuery(KCC kcc, KCCData data)
		{
			kcc.SuppressProcessors<IKCCProcessor>();
		}

		public override void OnStay(KCC kcc, KCCData data)
		{
			data.ExternalForce = default;
		}
	}
}
