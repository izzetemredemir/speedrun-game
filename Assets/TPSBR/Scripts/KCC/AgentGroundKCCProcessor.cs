namespace TPSBR
{
	using UnityEngine;
	using Fusion.KCC;

	public sealed class AgentGroundKCCProcessor : BaseKCCProcessor
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private float _speedMultiplier = 1.0f;
		[SerializeField][Tooltip("Kinematic velocity is accelerated by calculated kinematic speed multiplied by this.")]
		private float _relativeKinematicAcceleration = 50.0f;
		[SerializeField][Tooltip("Kinematic velocity is decelerated by actual kinematic speed multiplied by this. The faster KCC moves, the more deceleration is applied.")]
		private float _proportionalKinematicFriction = 35.0f;
		[SerializeField][Tooltip("Dynamic velocity is decelerated by actual dynamic speed multiplied by this. The faster KCC moves, the more deceleration is applied.")]
		private float _proportionalDynamicFriction = 20.0f;
		[SerializeField][Range(0.0f, 1.0f)][Tooltip("How fast input direction propagates to kinematic direction.")]
		private float _inputResponsivity = 1.0f;
		[SerializeField][Tooltip("Custom jump multiplier.")]
		private float _jumpMultiplier = 1.0f;

		private MoveState _moveState;

		// KCCProcessor INTERFACE

		public override float Priority => 2000;

		public override EKCCStages GetValidStages(KCC kcc, KCCData data)
		{
			EKCCStages stages = EKCCStages.ProcessPhysicsQuery;

			if (data.IsGrounded == true)
			{
				stages |= EKCCStages.SetDynamicVelocity;
//				stages |= EKCCStages.SetKinematicDirection; // Handled by SetKinematicSpeed (server performance optimizations)
//				stages |= EKCCStages.SetKinematicTangent;   // Handled by SetKinematicSpeed (server performance optimizations)
				stages |= EKCCStages.SetKinematicSpeed;
				stages |= EKCCStages.SetKinematicVelocity;
			}

			return stages;
		}

		public override void SetDynamicVelocity(KCC kcc, KCCData data)
		{
			if (data.IsSteppingUp == false && (data.IsSnappingToGround == true || data.GroundDistance > 0.001f))
			{
				data.DynamicVelocity += data.Gravity * data.DeltaTime;
			}

			if (data.JumpImpulse.IsZero() == false && _jumpMultiplier > 0.0f)
			{
				Vector3 jumpDirection = data.JumpImpulse.normalized;

				data.DynamicVelocity -= Vector3.Scale(data.DynamicVelocity, jumpDirection);
				data.DynamicVelocity += (data.JumpImpulse / kcc.Settings.Mass) * _jumpMultiplier;

				data.HasJumped = true;
			}

			data.DynamicVelocity += data.ExternalVelocity;
			data.DynamicVelocity += data.ExternalAcceleration * data.DeltaTime;
			data.DynamicVelocity += (data.ExternalImpulse / kcc.Settings.Mass);
			data.DynamicVelocity += (data.ExternalForce / kcc.Settings.Mass) * data.DeltaTime;

			if (data.DynamicVelocity.IsZero() == false)
			{
				if (data.DynamicVelocity.IsAlmostZero(0.001f) == true)
				{
					data.DynamicVelocity = default;
				}
				else
				{
					Vector3 frictionAxis = Vector3.one;
					if (data.GroundDistance > 0.001f || data.IsSnappingToGround == true)
					{
						frictionAxis.y = default;
					}

					data.DynamicVelocity += KCCPhysicsUtility.GetFriction(data.DynamicVelocity, data.DynamicVelocity, frictionAxis, data.GroundNormal, data.KinematicSpeed, true, 0.0f, 0.0f, _proportionalDynamicFriction, data.DeltaTime, kcc.FixedData.DeltaTime);
				}
			}
		}

		public override void SetKinematicSpeed(KCC kcc, KCCData data)
		{
			Vector3 inputDirectionXZ     = data.InputDirection.OnlyXZ();
			Vector3 kinematicDirectionXZ = data.KinematicDirection.OnlyXZ();

			data.KinematicDirection = KCCUtility.EasyLerpDirection(kinematicDirectionXZ, inputDirectionXZ, data.DeltaTime, _inputResponsivity);

			data.KinematicTangent = default;

			if (data.KinematicDirection.IsAlmostZero(0.0001f) == false && KCCPhysicsUtility.ProjectOnGround(data.GroundNormal, data.KinematicDirection, out Vector3 projectedMoveDirection) == true)
			{
				data.KinematicTangent = projectedMoveDirection.normalized;
			}
			else
			{
				data.KinematicTangent = data.GroundTangent;
			}

			data.KinematicSpeed = _moveState.GetBaseSpeed((Quaternion.Inverse(data.TransformRotation) * data.KinematicDirection).XZ0().normalized, default);
			data.KinematicSpeed *= _speedMultiplier;
		}

		public override void SetKinematicVelocity(KCC kcc, KCCData data)
		{
			if (data.KinematicVelocity.IsAlmostZero() == false && KCCPhysicsUtility.ProjectOnGround(data.GroundNormal, data.KinematicVelocity, out Vector3 projectedKinematicVelocity) == true)
			{
				data.KinematicVelocity = projectedKinematicVelocity.normalized * data.KinematicVelocity.magnitude;
			}

			if (data.KinematicDirection.IsAlmostZero() == true)
			{
				data.KinematicVelocity += KCCPhysicsUtility.GetFriction(data.KinematicVelocity, data.KinematicVelocity, Vector3.one, data.GroundNormal, data.KinematicSpeed, true, 0.0f, 0.0f, _proportionalKinematicFriction, data.DeltaTime, kcc.FixedData.DeltaTime);
				return;
			}

			Vector3 kinematicVelocity = data.KinematicVelocity;

			Vector3 moveDirection = kinematicVelocity;
			if (moveDirection.IsZero() == true)
			{
				moveDirection = data.KinematicTangent;
			}

			Vector3 acceleration = KCCPhysicsUtility.GetAcceleration(kinematicVelocity, data.KinematicTangent, Vector3.one, data.KinematicSpeed, false, data.KinematicDirection.magnitude, 0.0f, _relativeKinematicAcceleration, 0.0f, data.DeltaTime, kcc.FixedData.DeltaTime);
			Vector3 friction     = KCCPhysicsUtility.GetFriction(kinematicVelocity, moveDirection, Vector3.one, data.GroundNormal, data.KinematicSpeed, false, 0.0f, 0.0f, _proportionalKinematicFriction, data.DeltaTime, kcc.FixedData.DeltaTime);

			kinematicVelocity = KCCPhysicsUtility.CombineAccelerationAndFriction(kinematicVelocity, acceleration, friction);

			if (data.HasJumped == true && kinematicVelocity.y < 0.0f)
			{
				kinematicVelocity.y = 0.0f;
			}

			data.KinematicVelocity = kinematicVelocity;
		}

		public override void ProcessPhysicsQuery(KCC kcc, KCCData data)
		{
			if (data.IsGrounded == true)
			{
				if (data.WasGrounded == true && data.IsSnappingToGround == false && data.DynamicVelocity.y < 0.0f && data.DynamicVelocity.OnlyXZ().IsAlmostZero() == true)
				{
					data.DynamicVelocity.y = 0.0f;
				}

				if (data.WasGrounded == false)
				{
					if (data.KinematicVelocity.OnlyXZ().IsAlmostZero() == true)
					{
						data.KinematicVelocity.y = 0.0f;
					}
					else
					{
						if (KCCPhysicsUtility.ProjectOnGround(data.GroundNormal, data.KinematicVelocity, out Vector3 projectedKinematicVelocity) == true)
						{
							data.KinematicVelocity = projectedKinematicVelocity.normalized * data.KinematicVelocity.magnitude;
						}
					}
				}
			}
			else
			{
				if (data.IsGrounded == true || data.WasGrounded == true)
					return;

				if (data.DynamicVelocity.y > 0.0f)
				{
					Vector3 currentVelocity = (data.TargetPosition - data.BasePosition) / data.DeltaTime;
					if (currentVelocity.y.IsAlmostZero() == true)
					{
						data.DynamicVelocity.y = 0.0f;
					}
				}
			}
		}

		// MonoBehaviour INTERFACE

		public void Awake()
		{
			_moveState = gameObject.GetComponentInParent<CharacterAnimationController>().FindState<MoveState>();
		}
	}
}
