using Fusion;
using Fusion.KCC;
using Fusion.Animations;

namespace TPSBR
{
	using UnityEngine;

	[OrderAfter(typeof(KCC))]
	public sealed class CharacterAnimationController : AnimationController
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private Transform       _leftHand;
		[SerializeField]
		private Transform       _leftLowerArm;
		[SerializeField]
		private Transform       _leftUpperArm;
		[SerializeField][Range(0.0f, 1.0f)]
		private float           _aimSnapPower = 0.5f;

		private KCC             _kcc;
		private Agent           _agent;
		private Weapons         _weapons;
		private Jetpack         _jetpack;

		private LocomotionLayer _locomotion;
		private FullBodyLayer   _fullBody;
		private LowerBodyLayer  _lowerBody;
		private UpperBodyLayer  _upperBody;
		private ShootLayer      _shoot;
		private LookLayer       _look;

		// PUBLIC METHODS

		public bool CanJump()
		{
			if (_fullBody.IsActive() == true)
			{
				if (_fullBody.Jump.IsActive(true) == true)
					return false;
				if (_fullBody.Fall.IsActive(true) == true)
					return false;
				if (_fullBody.Dead.IsActive(true) == true)
					return false;
				if (_fullBody.Jetpack.IsActive(true) == true)
					return false;
			}

			return true;
		}

		public bool CanSwitchWeapons(bool force)
		{
			if (_fullBody.IsActive() == true)
			{
				if (_fullBody.Dead.IsActive() == true)
					return false;
				if (_fullBody.Jetpack.IsActive() == true)
					return false;
			}

			if (_upperBody.IsActive() == true)
			{
				if (_upperBody.Grenade.IsActive() == true && _upperBody.Grenade.CanSwitchWeapon() == false)
					return false;
				if (force == false && (_upperBody.Equip.IsActive() == true || _upperBody.Unequip.IsActive() == true))
					return false;
			}

			return true;
		}

		public void SetDead(bool isDead)
		{
			if (isDead == true)
			{
				_fullBody.Dead.Activate(0.2f);

				if (_kcc.Data.IsGrounded == true)
				{
					_kcc.SetLayer(LayerMask.NameToLayer("Ignore Raycast"));
					_kcc.SetLayerMask(_kcc.Settings.CollisionLayerMask & ~(1 << LayerMask.NameToLayer("AgentKCC")));
				}

				_upperBody.DeactivateAllStates(0.2f);
				_look.DeactivateAllStates(0.2f);
			}
			else
			{
				_fullBody.Dead.Deactivate(0.2f);
				_kcc.SetShape(EKCCShape.Capsule);
			}
		}

		public bool StartFire()
		{
			if (_fullBody.Dead.IsActive() == true)
					return false;
			if (_upperBody.HasActiveState() == true)
				return false;

			_shoot.Shoot.AnimationTime = 0.0f;
			_shoot.Shoot.Activate(0.2f);
			return true;
		}

		public void ProcessThrow(bool start, bool hold)
		{
			_upperBody.Grenade.ProcessThrow(start, hold);
		}

		public bool StartReload()
		{
			if (_upperBody.Grenade.IsActive() == true)
				return _upperBody.Grenade.ProcessReload();

			if (_fullBody.Dead.IsActive() == true)
				return false;
			if (_upperBody.Reload.IsActive() == true)
				return true;
			if (_upperBody.HasActiveState() == true)
				return false;

			_upperBody.Reload.Activate(0.2f);
			return true;
		}

		public void SwitchWeapons()
		{
			_upperBody.Reload.Deactivate(0.2f);

			if (_weapons.PendingWeapon is ThrowableWeapon)
			{
				_upperBody.Grenade.Equip();
				return;
			}

			if (_weapons.PendingWeaponSlot > 0)
			{
				_weapons.DisarmCurrentWeapon();

				_upperBody.Equip.AnimationTime = 0.0f;
				_upperBody.Equip.Activate(0.2f);
			}
			else
			{
				_upperBody.Unequip.AnimationTime = 0.0f;
				_upperBody.Unequip.Activate(0.2f);
			}
		}

		public void Turn(float angle)
		{
			_lowerBody.Turn.Refresh(angle);
		}

		public void RefreshSnapping()
		{
			SnapWeapon();
		}

		// AnimationController INTERFACE

		protected override void OnSpawned()
		{
			if (HasStateAuthority == true)
			{
				Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
			}

			_locomotion.Move.Activate(0.0f);

			if (_weapons.IsSwitchingWeapon() == true)
			{
				SwitchWeapons();
			}
		}

		protected override void OnFixedUpdate()
		{
			if (_jetpack.IsActive == true && _fullBody.Jetpack.IsActive() == false)
			{
				_upperBody.Reload.Deactivate(0.2f);

				_weapons.DisarmCurrentWeapon();

				_fullBody.Jetpack.Activate(0.1f);
			}
			else if (_jetpack.IsActive == false && _fullBody.Jetpack.IsActive() == true)
			{
				_fullBody.Jetpack.Deactivate(0.1f);

				SwitchWeapons(); // Equip pending weapon
			}
		}

		protected override void OnEvaluate()
		{
			SnapWeapon();
		}

		// MonoBehaviour INTERFACE

		protected override void Awake()
		{
			base.Awake();

			_kcc        = this.GetComponentNoAlloc<KCC>();
			_agent      = this.GetComponentNoAlloc<Agent>();
			_weapons    = this.GetComponentNoAlloc<Weapons>();
			_jetpack    = this.GetComponentNoAlloc<Jetpack>();

			_locomotion = FindLayer<LocomotionLayer>();
			_fullBody   = FindLayer<FullBodyLayer>();
			_lowerBody  = FindLayer<LowerBodyLayer>();
			_upperBody  = FindLayer<UpperBodyLayer>();
			_shoot      = FindLayer<ShootLayer>();
			_look       = FindLayer<LookLayer>();
		}

		// PRIVATE METHODS

		private void SnapWeapon()
		{
			if (ApplicationSettings.IsBatchServer == true)
				return;
			if (_weapons.CurrentWeapon == null || CanSnapHand() == false)
				return;

			Transform weaponHandle = _weapons.WeaponHandle;
			if (HasInputAuthority == true)
			{
				weaponHandle.localRotation = _weapons.WeaponBaseRotation;

				Quaternion handleRotation = weaponHandle.rotation;
				Quaternion targetRotation = Quaternion.LookRotation(_agent.Context.Camera.transform.position + _agent.Context.Camera.transform.forward * 100.0f - weaponHandle.position);

				float   snapPower    = Mathf.Clamp(Mathf.Abs(_kcc.FixedData.LookPitch) / 60.0f, _aimSnapPower, 1.0f);
				Vector3 snapRotation = Quaternion.Slerp(handleRotation, targetRotation, snapPower).eulerAngles;

				snapRotation.y = targetRotation.eulerAngles.y;

				weaponHandle.rotation = Quaternion.Euler(snapRotation);
			}
			else
			{
				weaponHandle.rotation = Quaternion.LookRotation(_kcc.FixedData.LookDirection);
			}

			Transform leftHandTarget = _weapons.CurrentWeapon.LeftHandTarget;
			if (leftHandTarget != null)
			{
				bool leftSide = _agent.LeftSide;

				Vector3    leftHandLocalPosition       = _leftLowerArm.InverseTransformPoint(_leftHand.position);
				Vector3    leftHandTargetLocalPosition = _leftLowerArm.InverseTransformPoint(leftHandTarget.position);
				Quaternion leftLowerArmRotation        = Quaternion.FromToRotation(leftHandLocalPosition, leftHandTargetLocalPosition);

				_leftLowerArm.rotation *= leftSide == true ? Quaternion.Inverse(leftLowerArmRotation) : leftLowerArmRotation;

				for (int i = 0; i < 2; ++i)
				{
					Vector3    leftLowerArmOffset              = leftHandTarget.position - _leftHand.position;
					Vector3    leftLowerArmTargetPosition      = _leftLowerArm.position + leftLowerArmOffset;
					Vector3    leftLowerArmLocalPosition       = _leftUpperArm.InverseTransformPoint(_leftLowerArm.position);
					Vector3    leftLowerArmTargetLocalPosition = _leftUpperArm.InverseTransformPoint(leftLowerArmTargetPosition);
					Quaternion leftUpperArmRotation            = Quaternion.FromToRotation(leftLowerArmLocalPosition, leftLowerArmTargetLocalPosition);

					_leftUpperArm.rotation *= leftSide == true ? Quaternion.Inverse(leftUpperArmRotation) : leftUpperArmRotation;

					leftHandLocalPosition       = _leftLowerArm.InverseTransformPoint(_leftHand.position);
					leftHandTargetLocalPosition = _leftLowerArm.InverseTransformPoint(leftHandTarget.position);
					leftLowerArmRotation        = Quaternion.FromToRotation(leftHandLocalPosition, leftHandTargetLocalPosition);

					_leftLowerArm.rotation *= leftSide == true ? Quaternion.Inverse(leftLowerArmRotation) : leftLowerArmRotation;
				}

				_leftHand.position = leftHandTarget.position;
				_leftHand.rotation = leftHandTarget.rotation;
			}
		}

		private bool CanSnapHand()
		{
			if (_fullBody.Dead.IsActive() == true || _fullBody.Jetpack.IsActive() == true)
				return false;

			if (_upperBody.HasActiveState() == true)
			{
				if (_upperBody.Reload.IsFinished(0.85f) == true)
					return true;
				if (_upperBody.Equip.IsFinished(0.75f) == true)
					return true;

				return false;
			}

			return true;
		}
	}
}
