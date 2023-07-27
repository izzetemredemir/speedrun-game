using Fusion;
using Fusion.KCC;
using UnityEngine;

namespace TPSBR
{
	public class Airplane : ContextBehaviour
	{
		// PUBLIC MEMBERS

		[Networked, HideInInspector]
		public Vector3     NextTargetPosition   { get; set; }

		public bool        DropEnabled          { get { return _state.IsBitSet(0); } set { _state = _state.SetBitNoRef(0, value); } }
		public bool        DropFinished         { get { return _state.IsBitSet(1); } set { _state = _state.SetBitNoRef(1, value); } }
		public bool        IsFlyingAway         { get { return _state.IsBitSet(2); } set { _state = _state.SetBitNoRef(2, value); } }
		public bool        IsFinished           { get { return _state.IsBitSet(3); } set { _state = _state.SetBitNoRef(3, value); } }

		public Transform   AgentPosition        => _agentPosition;
		public float       OutZoneDistance      => _outZoneDistance;

		// PRIVATE MEMBERS

		[SerializeField]
		private GameObject _dropEnabledObject;
		[SerializeField]
		private GameObject _dropDisabledObject;
		[SerializeField]
		private Transform _agentPosition;
		[SerializeField]
		private float _maxSpeed = 8f;
		[SerializeField]
		private float _maxAcceleration = 8f;
		[SerializeField]
		private float _tiltSpeed = 8f;
		[SerializeField]
		private float _maxTilt = 60;
		[SerializeField]
		private float _minDistanceToTargetChange = 60f;
		[SerializeField]
		private float _outZoneDistance = 20f;
		[SerializeField]
		private float _inZoneDistance = 10f;

		[Header("Sounds")]
		[SerializeField]
		private AudioEffect _audioEffect;
		[SerializeField]
		private AudioSetup _playerJumpedSound;

		[Networked]
		private byte _state { get; set; }
		[Networked]
		private Vector3 _velocity { get; set; }
		[Networked]
		private float _tilt { get; set; }

		// PUBLIC METHODS

		public void ActivateDropWindow()
		{
			if (Object.HasStateAuthority == false)
				return;

			DropEnabled = true;
			NextTargetPosition = Vector3.zero;
		}

		public void DeactivateDropWindow()
		{
			if (Object.HasStateAuthority == false)
				return;

			DropEnabled = false;
			DropFinished = true;
			NextTargetPosition = Vector3.zero;
		}

		public void OnPlayerJumped(PlayerRef playerRef)
		{
			_audioEffect.Play(_playerJumpedSound);
		}

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			if (Object.HasStateAuthority == false)
				return;

			_velocity = transform.rotation * (Vector3.forward * _maxSpeed);
		}

		public override void FixedUpdateNetwork()
		{
			if (Object.HasStateAuthority == false)
				return;

			if (IsFinished == true)
				return;

			if (NeedsNewTarget() == true)
			{
				if (IsFlyingAway == true)
				{
					// Plane is near fly away target, let's despawn
					IsFinished = true;
					return;
				}
				else if (DropEnabled == true)
				{
					NextTargetPosition = FindNewTargetInZone();
				}
				else if (DropFinished == false)
				{
					// Waiting for drop start
					NextTargetPosition = FindNewTargetOutsideZone();
				}
				else
				{
					NextTargetPosition = FindFlyAwayTarget();
					IsFlyingAway = true;
				}
			}

			UpdatePosition();
		}

		public override void Render()
		{
			_dropEnabledObject.SetActive(DropEnabled);
			_dropDisabledObject.SetActive(DropEnabled == false);
		}

		// PRIVATE METHODS

		private bool NeedsNewTarget()
		{
			if (NextTargetPosition == Vector3.zero)
				return true;

			return Vector3.SqrMagnitude(transform.position - NextTargetPosition) < _minDistanceToTargetChange * _minDistanceToTargetChange;
		}

		private Vector3 FindNewTargetInZone()
		{
			var area = Context.GameplayMode.ShrinkingArea;

			var dirToPlane = (transform.position - area.Center).OnlyXZ().normalized;
			var nextDirection = -dirToPlane + _velocity.OnlyXZ().normalized * 0.3f;

			if (nextDirection == Vector3.zero)
			{
				nextDirection = -dirToPlane;
				nextDirection.x += 5;
			}

			var areaPoint = area.Center + nextDirection.normalized * (area.Radius - _inZoneDistance);
			areaPoint.y = transform.position.y;

			return areaPoint;
		}

		private Vector3 FindNewTargetOutsideZone()
		{
			var area = Context.GameplayMode.ShrinkingArea;
			var dirOutOfArea = (transform.position + transform.forward * 60f - area.Center).OnlyXZ().normalized;

			var areaPoint = area.Center + dirOutOfArea * (area.Radius + _outZoneDistance);
			areaPoint.y = transform.position.y;

			return areaPoint;
		}

		private Vector3 FindFlyAwayTarget()
		{
			var area = Context.GameplayMode.ShrinkingArea;
			var dirOutOfArea = (transform.position + transform.forward * 60f - area.Center).OnlyXZ().normalized;

			var areaPoint = area.Center + dirOutOfArea * (area.Radius + 300f);
			areaPoint.y = transform.position.y + 50f;

			return areaPoint;
		}

		private void UpdatePosition()
		{
			float deltaTime = Runner.DeltaTime;

			transform.position = transform.position + _velocity * deltaTime;
			var directionToTarget = NextTargetPosition - transform.position;

			var lastLeft = -transform.right;
			lastLeft.y = 0f;
			float directionDot = Vector3.Dot(directionToTarget.normalized, lastLeft.normalized);
			float targetTilt = MathUtility.Map(-1f, 1f, -_maxTilt, _maxTilt, directionDot);

			_tilt = Mathf.Lerp(_tilt, targetTilt, _tiltSpeed * deltaTime);

			var newRotation = Quaternion.LookRotation(_velocity, Vector3.up).eulerAngles;
			newRotation.z = _tilt;
			transform.rotation = Quaternion.Euler(newRotation);

			_velocity += directionToTarget.normalized * _maxAcceleration * deltaTime;

			float speed = _velocity.magnitude;
			if (speed > _maxSpeed)
			{
				_velocity = (_velocity / speed) * _maxSpeed;
			}
		}
	}
}
