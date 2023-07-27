using Fusion.Animations;
using Fusion.KCC;

namespace TPSBR
{
	using UnityEngine;

	public sealed class MoveState : MultiBlendTreeState
	{
		// PUBLIC MEMBERS

		public Vector3 FixedDirection        => _fixedDirection;
		public float   FixedMagnitude        => _fixedMagnitude;
		public Vector3 InterpolatedDirection => _interpolatedDirection;
		public float   InterpolatedMagnitude => _interpolatedMagnitude;

		// PRIVATE MEMBERS

		[SerializeField]
		private float   _minAnimationSpeed       = 0.25f;
		[SerializeField]
		private float   _maxAnimationSpeed       = 2.0f;
		[SerializeField]
		private float   _directionSmoothingSpeed = 16.0f;
		[SerializeField]
		private float   _magnitudeSmoothingSpeed = 16.0f;

		private KCC     _kcc;
		private Agent   _agent;
		private Weapons _weapons;
		private Vector3 _fixedDirection;
		private float   _fixedMagnitude;
		private Vector3 _interpolatedDirection;
		private float   _interpolatedMagnitude;

		// PUBLIC METHODS

		public float GetBaseSpeed(Vector2 localNormalizedDirection, float multiplier)
		{
			if (multiplier == default)
			{
				multiplier = GetMultiplier();
			}

			return GetMaxBaseSpeed(localNormalizedDirection) * multiplier;
		}

		// MultiBlendTreeState INTERFACE

		public override Vector2 GetBlendPosition(bool interpolated)
		{
			Vector3 direction = interpolated == true ? _interpolatedDirection : _fixedDirection;
			float   magnitude = interpolated == true ? _interpolatedMagnitude : _fixedMagnitude;

			Vector3 blendPosition = _kcc.transform.InverseTransformDirection(direction).XZ0().normalized * magnitude;

			if (_agent != null && _agent.Runner != null && _agent.LeftSide == true)
			{
				blendPosition.x = -blendPosition.x;
			}

			return blendPosition;
		}

		public override float GetSpeedMultiplier()
		{
			float maxBaseSpeed = GetMaxBaseSpeed((Quaternion.Inverse(_kcc.FixedData.TransformRotation) * _kcc.FixedData.KinematicDirection).XZ0().normalized);
			if (maxBaseSpeed > 0.0f && _kcc.FixedData.RealSpeed > maxBaseSpeed)
					return Mathf.Clamp(_kcc.FixedData.RealSpeed / maxBaseSpeed, _minAnimationSpeed, _maxAnimationSpeed);

			return 1.0f;
		}

		protected override int GetSetID()
		{
			int currentWeaponSlot = _weapons.CurrentWeaponSlot;
			if (currentWeaponSlot > 2)
			{
				currentWeaponSlot = 1; // For grenades we use pistol set
			}

			if (currentWeaponSlot < 0)
				return 0;

			return currentWeaponSlot;
		}

		// AnimationState INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_kcc     = Controller.GetComponentNoAlloc<KCC>();
			_agent   = Controller.GetComponentNoAlloc<Agent>();
			_weapons = Controller.GetComponentNoAlloc<Weapons>();
		}

		protected override void OnSpawned()
		{
			base.OnSpawned();

			_fixedDirection        = Vector3.forward;
			_fixedMagnitude        = 0.0f;
			_interpolatedDirection = Vector3.forward;
			_interpolatedMagnitude = 0.0f;
		}

		protected override void OnFixedUpdate()
		{
			SetFixedProperties();

			base.OnFixedUpdate();
		}

		protected override void OnInterpolate()
		{
			SetInterpolatedProperties();

			base.OnInterpolate();
		}

		// PRIVATE METHODS

		private void SetFixedProperties()
		{
			KCCData kccFixedData    = _kcc.FixedData;
			Vector3 targetDirection = _fixedDirection;
			float   targetMagnitude;

			if (Controller.HasInputAuthority == true || Controller.HasStateAuthority == true)
			{
				if (kccFixedData.InputDirection.OnlyXZ().IsAlmostZero(0.025f) == false)
				{
					targetDirection = kccFixedData.InputDirection.OnlyXZ().normalized;
				}
				else if (kccFixedData.KinematicDirection.OnlyXZ().IsAlmostZero(0.025f) == false)
				{
					targetDirection = kccFixedData.KinematicDirection.OnlyXZ().normalized;
				}
				else if (kccFixedData.DesiredVelocity.OnlyXZ().IsAlmostZero(0.025f) == false)
				{
					targetDirection = kccFixedData.DesiredVelocity.OnlyXZ().normalized;
				}

				float realVelocityMagnitude      = kccFixedData.RealVelocity.OnlyXZ().magnitude;
				float desiredVelocityMagnitude   = kccFixedData.DesiredVelocity.OnlyXZ().magnitude;
				float kinematicVelocityMagnitude = kccFixedData.KinematicVelocity.OnlyXZ().magnitude;

				targetMagnitude = Mathf.Min(realVelocityMagnitude, Mathf.Max(kinematicVelocityMagnitude, desiredVelocityMagnitude));
			}
			else
			{
				if (kccFixedData.RealVelocity.OnlyXZ().IsAlmostZero(0.025f) == false)
				{
					targetDirection = kccFixedData.RealVelocity.OnlyXZ().normalized;
				}

				targetMagnitude = kccFixedData.RealSpeed;
			}

			_fixedDirection = targetDirection;
			_fixedMagnitude = targetMagnitude;
		}

		private void SetInterpolatedProperties()
		{
			KCCData kccFixedData    = _kcc.FixedData;
			KCCData kccRenderData   = _kcc.RenderData;
			Vector3 targetDirection = _interpolatedDirection;
			float   targetMagnitude;

			if (Controller.HasInputAuthority == true || Controller.HasStateAuthority == true)
			{
				if (kccRenderData.InputDirection.OnlyXZ().IsAlmostZero(0.025f) == false)
				{
					targetDirection = kccRenderData.InputDirection.OnlyXZ().normalized;
				}
				else if (kccRenderData.KinematicDirection.OnlyXZ().IsAlmostZero(0.025f) == false)
				{
					targetDirection = kccRenderData.KinematicDirection.OnlyXZ().normalized;
				}
				else if (kccFixedData.DesiredVelocity.OnlyXZ().IsAlmostZero(0.025f) == false)
				{
					targetDirection = kccFixedData.DesiredVelocity.OnlyXZ().normalized;
				}

				float directionDot = Vector3.Dot(targetDirection, _interpolatedDirection);
				if (directionDot < -0.5f)
				{
					const float angleTolerance = 25.0f;

					float angle = Vector3.SignedAngle(_kcc.transform.forward, _interpolatedDirection, Vector3.up);
					if (angle.AlmostEquals(90.0f, angleTolerance) == true)
					{
						targetDirection = Quaternion.Euler(0.0f, -90.0f, 0.0f) * _interpolatedDirection;
					}
					else if (angle.AlmostEquals(-90.0f, angleTolerance) == true)
					{
						targetDirection = Quaternion.Euler(0.0f, 90.0f, 0.0f) * _interpolatedDirection;
					}
					else if (angle.AlmostEquals(0.0f, angleTolerance) == true)
					{
						targetDirection = Quaternion.Euler(0.0f, -90.0f, 0.0f) * _interpolatedDirection;
					}
					else if (Mathf.Abs(angle).AlmostEquals(180.0f, angleTolerance) == true)
					{
						targetDirection = Quaternion.Euler(0.0f, 90.0f, 0.0f) * _interpolatedDirection;
					}
				}

				float realVelocityMagnitude      = kccFixedData.RealVelocity.OnlyXZ().magnitude;
				float desiredVelocityMagnitude   = kccFixedData.DesiredVelocity.OnlyXZ().magnitude;
				float kinematicVelocityMagnitude = kccFixedData.KinematicVelocity.OnlyXZ().magnitude;

				targetMagnitude = Mathf.Min(realVelocityMagnitude, Mathf.Max(kinematicVelocityMagnitude, desiredVelocityMagnitude));
			}
			else
			{
				if (kccFixedData.RealVelocity.OnlyXZ().IsAlmostZero(0.025f) == false)
				{
					targetDirection = kccFixedData.RealVelocity.OnlyXZ().normalized;
					targetMagnitude = kccFixedData.RealSpeed;
				}
				else
				{
					targetMagnitude = 0.0f;
				}
			}

			_interpolatedDirection = Vector3.Slerp(_interpolatedDirection, targetDirection, _directionSmoothingSpeed * Time.deltaTime).normalized;
			_interpolatedMagnitude = Mathf.Lerp(_interpolatedMagnitude, targetMagnitude, _magnitudeSmoothingSpeed * Time.deltaTime);
		}

		private float GetMaxBaseSpeed(Vector2 localNormalizedDirection)
		{
			if (localNormalizedDirection == Vector2.zero)
				return 0.0f;

			int setID = GetSetID();
			if (setID < 0)
				return 0.0f;

			BlendTreeNode[] nodes = Sets[setID].Nodes;
			int   fromNodeIndex;
			int   toNodeIndex;
			float alpha;

			float angle = Vector2.Angle(localNormalizedDirection, Vector2.up);
			if (angle >= 0.0f)
			{
				if      (angle <=  45.0f) { fromNodeIndex = 1; toNodeIndex = 6; alpha = Mathf.Clamp01((angle -   0.0f) / 45.0f); }
				else if (angle <=  90.0f) { fromNodeIndex = 6; toNodeIndex = 4; alpha = Mathf.Clamp01((angle -  45.0f) / 45.0f); }
				else if (angle <= 135.0f) { fromNodeIndex = 4; toNodeIndex = 8; alpha = Mathf.Clamp01((angle -  90.0f) / 45.0f); }
				else                      { fromNodeIndex = 8; toNodeIndex = 2; alpha = Mathf.Clamp01((angle - 135.0f) / 45.0f); }
			}
			else
			{
				if      (angle >=  -45.0f) { fromNodeIndex = 1; toNodeIndex = 5; alpha = Mathf.Clamp01((angle +   0.0f) / -45.0f); }
				else if (angle >=  -90.0f) { fromNodeIndex = 5; toNodeIndex = 3; alpha = Mathf.Clamp01((angle +  45.0f) / -45.0f); }
				else if (angle >= -135.0f) { fromNodeIndex = 3; toNodeIndex = 7; alpha = Mathf.Clamp01((angle +  90.0f) / -45.0f); }
				else                       { fromNodeIndex = 7; toNodeIndex = 2; alpha = Mathf.Clamp01((angle + 135.0f) / -45.0f); }
			}

			return Mathf.Lerp(nodes[fromNodeIndex].Position.magnitude, nodes[toNodeIndex].Position.magnitude, alpha);
		}

		private float GetMultiplier()
		{
			switch (_weapons.CurrentWeaponSlot)
			{
				case 0: { return 1.0f;  }
				case 1: { return 0.95f; }
				case 2: { return 0.9f;  }
			}

			return 0.95f;
		}
	}
}
