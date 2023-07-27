namespace TPSBR
{
	using UnityEngine;
	using Fusion.KCC;

	public sealed class AgentAirKCCProcessor : BaseKCCProcessor
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private float _speedMultiplier = 1.0f;
		[SerializeField][Tooltip("Kinematic velocity is accelerated by calculated kinematic speed multiplied by this.")]
		private float _relativeKinematicAcceleration = 5.0f;
		[SerializeField][Tooltip("Kinematic velocity is decelerated by actual kinematic speed multiplied by this. The faster KCC moves, the more deceleration is applied.")]
		private float _proportionalKinematicFriction = 2.0f;
		[SerializeField][Tooltip("Dynamic velocity is decelerated by actual dynamic speed multiplied by this. The faster KCC moves, the more deceleration is applied.")]
		private float _proportionalDynamicFriction = 2.0f;
		[SerializeField][Range(0.0f, 1.0f)][Tooltip("How fast input direction propagates to kinematic direction.")]
		private float _inputResponsivity = 0.75f;
		[SerializeField][Tooltip("Custom gravity multiplier when moving up.")]
		private float _upGravityMultiplier = 1.0f;
		[SerializeField][Tooltip("Custom gravity multiplier when moving down.")]
		private float _downGravityMultiplier = 1.0f;

		private MoveState _moveState;

		// KCCProcessor INTERFACE

		public override float Priority => 1000;

		public override EKCCStages GetValidStages(KCC kcc, KCCData data)
		{
//			EKCCStages stages = EKCCStages.ProcessPhysicsQuery; // Handled by AgentGroundKCCProcessor (server performance optimizations)
			EKCCStages stages = EKCCStages.None;

			if (data.IsGrounded == false)
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
			float gravityMultiplier = data.RealVelocity.y > 0.0f ? _upGravityMultiplier : _downGravityMultiplier;

			data.DynamicVelocity += data.Gravity * gravityMultiplier * data.DeltaTime;
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
					data.DynamicVelocity += KCCPhysicsUtility.GetFriction(data.DynamicVelocity, data.DynamicVelocity, new Vector3(1.0f, 0.0f, 1.0f), data.KinematicSpeed, true, 0.0f, 0.0f, _proportionalDynamicFriction, data.DeltaTime, kcc.FixedData.DeltaTime);
				}
			}
		}

		public override void SetKinematicSpeed(KCC kcc, KCCData data)
		{
			Vector3 inputDirectionXZ     = data.InputDirection.OnlyXZ();
			Vector3 kinematicDirectionXZ = data.KinematicDirection.OnlyXZ();

			data.KinematicDirection = KCCUtility.EasyLerpDirection(kinematicDirectionXZ, inputDirectionXZ, data.DeltaTime, _inputResponsivity);

			data.KinematicTangent = default;

			if (data.KinematicDirection.IsAlmostZero(0.0001f) == false)
			{
				data.KinematicTangent = data.KinematicDirection.normalized;
			}
			else
			{
				data.KinematicTangent = data.TransformDirection;
			}

			data.KinematicSpeed = _moveState.GetBaseSpeed((Quaternion.Inverse(data.TransformRotation) * data.KinematicDirection).XZ0().normalized, default);
			data.KinematicSpeed *= _speedMultiplier;
		}

		public override void SetKinematicVelocity(KCC kcc, KCCData data)
		{
			if (data.KinematicDirection.IsZero() == true)
			{
				data.KinematicVelocity += KCCPhysicsUtility.GetFriction(data.KinematicVelocity, data.KinematicVelocity, new Vector3(1.0f, 0.0f, 1.0f), data.KinematicSpeed, true, 0.0f, 0.0f, _proportionalKinematicFriction, data.DeltaTime, kcc.FixedData.DeltaTime);
				return;
			}

			Vector3 kinematicVelocity = data.KinematicVelocity;

			Vector3 moveDirection = kinematicVelocity;
			if (moveDirection.IsZero() == true)
			{
				moveDirection = data.KinematicTangent;
			}

			Vector3 acceleration = KCCPhysicsUtility.GetAcceleration(kinematicVelocity, data.KinematicTangent, Vector3.one, data.KinematicSpeed, false, data.KinematicDirection.magnitude, 0.0f, _relativeKinematicAcceleration, 0.0f, data.DeltaTime, kcc.FixedData.DeltaTime);
			Vector3 friction     = KCCPhysicsUtility.GetFriction(kinematicVelocity, moveDirection, new Vector3(1.0f, 0.0f, 1.0f), data.KinematicSpeed, false, 0.0f, 0.0f, _proportionalKinematicFriction, data.DeltaTime, kcc.FixedData.DeltaTime);

			kinematicVelocity = KCCPhysicsUtility.CombineAccelerationAndFriction(kinematicVelocity, acceleration, friction);

			data.KinematicVelocity = kinematicVelocity;
		}

		// MonoBehaviour INTERFACE

		public void Awake()
		{
			_moveState = gameObject.GetComponentInParent<CharacterAnimationController>().FindState<MoveState>();
		}
	}
}
