using System;
using Fusion;
using Fusion.KCC;
using UnityEngine;

namespace TPSBR
{
	[RequireComponent(typeof(Rigidbody))]
	[OrderBefore(typeof(EarlyAgentController), typeof(NetworkTransformAnchor))]
	public class Elevator : ContextAreaOfInterestBehaviour, IBeforeAllTicks, IKCCProcessor, IKCCProcessorProvider
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private float _height = -5f;
		[SerializeField]
		private float _speed = 1f;
		[SerializeField]
		private Collider _snapVolume;
		[SerializeField]
		private float _spaceTransitionSpeed = 4.0f;

		[Networked, Accuracy(AccuracyDefaults.POSITION)]
		private Vector3 _position { get; set; }
		[Networked, Accuracy(AccuracyDefaults.ROTATION)]
		private Quaternion _rotation { get; set; }
		[Networked, Accuracy(AccuracyDefaults.POSITION)]
		private Vector3 _basePosition { get; set; }
		[Networked]
		private int _direction { get; set; }
		[Networked]
		private float _currentHeight { get; set; }
		[Networked, Capacity(8)]
		protected NetworkArray<ElevatorEntity> _entities { get; }

		private Transform       _transform;
		private Rigidbody       _rigidbody;
		private float           _renderTime;
		private int             _renderDirection;
		private Vector3         _renderPosition;
		private RawInterpolator _entitiesInterpolator;

		// PUBLIC METHODS

		public void OverrideHeight(float height)
		{
			_currentHeight = height;
		}

		// NetworkAreaOfInterestBehaviour INTERFACE

		public override int PositionWordOffset => 0;

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			if (Object.HasStateAuthority == true)
			{
				_position = _transform.position;
				_rotation = _transform.rotation;
				_basePosition = _position;
				_direction = default;
				_currentHeight = _height;
			}

			_renderTime           = Runner.SimulationTime;
			_renderPosition       = _position;
			_renderDirection      = _direction;
			_entitiesInterpolator = GetInterpolator(nameof(_entities));

			if (ApplicationSettings.IsStrippedBatch == true)
			{
				gameObject.SetActive(false);
			}
		}

		public override void FixedUpdateNetwork()
		{
			CalculateNextPosition(_direction, _position, Runner.DeltaTime, out int nextDirection, out Vector3 positionDelta);

			_position += positionDelta;
			_direction = nextDirection;

			_renderTime      = Runner.SimulationTime;
			_renderPosition  = _position;
			_renderDirection = _direction;

			_transform.position = _position;
			_rigidbody.position = _position;

			if (_entities.Length <= 0)
				return;

			if (Object.HasStateAuthority == true)
			{
				for (int i = 0; i < _entities.Length; ++i)
				{
					ElevatorEntity entity = _entities.Get(i);
					if (entity.SpaceAlpha > 0.0f)
					{
						entity.SpaceAlpha = Mathf.Max(0.0f, entity.SpaceAlpha - Runner.DeltaTime * _spaceTransitionSpeed);
						if (entity.SpaceAlpha == 0.0f)
						{
							entity.Id     = default;
							entity.Offset = default;
						}

						_entities.Set(i, entity);
					}
				}
			}

			ApplyPositionDelta(positionDelta);
		}

		public override void Render()
		{
			float renderTime = Runner.SimulationTime + Runner.DeltaTime * Runner.Simulation.StateAlpha;
			float deltaTime  = renderTime - _renderTime;

			CalculateNextPosition(_renderDirection, _renderPosition, deltaTime, out int nextDirection, out Vector3 positionDelta);

			_renderTime      = renderTime;
			_renderPosition += positionDelta;
			_renderDirection = nextDirection;

			_transform.position = _renderPosition;
			_rigidbody.position = _renderPosition;

			ApplyPositionDelta(positionDelta);
		}

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			_transform = transform;
			_rigidbody = GetComponent<Rigidbody>();

			if (_rigidbody == null)
				throw new NullReferenceException($"GameObject {name} has missing Rigidbody component!");

			_rigidbody.isKinematic   = true;
			_rigidbody.useGravity    = false;
			_rigidbody.interpolation = RigidbodyInterpolation.None;
			_rigidbody.constraints   = RigidbodyConstraints.FreezeAll;
		}

		// IBeforeAllTicks INTERFACE

		void IBeforeAllTicks.BeforeAllTicks(bool resimulation, int tickCount)
		{
			if (resimulation == true)
			{
				_transform.SetPositionAndRotation(_position, _rotation);
			}
		}

		// IKCCProcessor INTERFACE

		float IKCCProcessor.Priority => float.MaxValue;

		EKCCStages IKCCProcessor.GetValidStages(KCC kcc, KCCData data)
		{
			return EKCCStages.SetInputProperties | EKCCStages.OnStay | EKCCStages.OnInterpolate;
		}

		void IKCCProcessor.SetInputProperties(KCC kcc, KCCData data)
		{
			kcc.SuppressFeature(EKCCFeature.PredictionCorrection);
		}

		void IKCCProcessor.SetDynamicVelocity(KCC kcc, KCCData data) {}
		void IKCCProcessor.SetKinematicDirection(KCC kcc, KCCData data) {}
		void IKCCProcessor.SetKinematicTangent(KCC kcc, KCCData data) {}
		void IKCCProcessor.SetKinematicSpeed(KCC kcc, KCCData data) {}
		void IKCCProcessor.SetKinematicVelocity(KCC kcc, KCCData data) {}
		void IKCCProcessor.ProcessPhysicsQuery(KCC kcc, KCCData data) {}
		void IKCCProcessor.OnEnter(KCC kcc, KCCData data) {}
		void IKCCProcessor.OnExit(KCC kcc, KCCData data) {}

		void IKCCProcessor.OnStay(KCC kcc, KCCData data)
		{
			if (kcc.IsInFixedUpdate == true && Object.HasStateAuthority == true && _snapVolume.ClosestPoint(data.TargetPosition).AlmostEquals(data.TargetPosition) == true)
			{
				for (int i = 0; i < _entities.Length; ++i)
				{
					ElevatorEntity entity = _entities.Get(i);
					if (entity.Id == kcc.Object.Id)
					{
						entity.Offset     = data.TargetPosition - _position;
						entity.SpaceAlpha = Mathf.Min(entity.SpaceAlpha + Runner.DeltaTime * _spaceTransitionSpeed * 2.0f, 1.0f);

						_entities.Set(i, entity);

						return;
					}
				}

				for (int i = 0; i < _entities.Length; ++i)
				{
					ElevatorEntity entity = _entities.Get(i);
					if (entity.Id == default)
					{
						entity.Id         = kcc.Object.Id;
						entity.Offset     = data.TargetPosition - _position;
						entity.SpaceAlpha = Runner.DeltaTime * _spaceTransitionSpeed + 0.001f;

						_entities.Set(i, entity);

						return;
					}
				}
			}
		}

		void IKCCProcessor.OnInterpolate(KCC kcc, KCCData data)
		{
			for (int i = 0; i < _entities.Length; ++i)
			{
				ElevatorEntity entity = _entities.Get(i);
				if (entity.Id == kcc.Object.Id)
				{
					if (_entitiesInterpolator.TryGetArray(_entities, out NetworkArray<ElevatorEntity> from, out NetworkArray<ElevatorEntity> to, out float alpha) == true)
					{
						ElevatorEntity fromEntity = from.Get(i);
						ElevatorEntity toEntity   = to.Get(i);

						Vector3 interpolatedOffset     = Vector3.Lerp(fromEntity.Offset, toEntity.Offset, alpha);
						float   interpolatedSpaceAlpha = Mathf.Lerp(fromEntity.SpaceAlpha, toEntity.SpaceAlpha, alpha);

						data.TargetPosition = Vector3.Lerp(data.TargetPosition, _transform.position + interpolatedOffset, interpolatedSpaceAlpha);
					}

					break;
				}
			}
		}

		void IKCCProcessor.ProcessUserLogic(KCC kcc, KCCData data, object userData)
		{
		}

		// IKCCInteractionProvider INTERFACE

		bool IKCCInteractionProvider.CanStartInteraction(KCC kcc, KCCData data) => true;
		bool IKCCInteractionProvider.CanStopInteraction (KCC kcc, KCCData data) => true;

		// IKCCProcessorProvider INTERFACE

		IKCCProcessor IKCCProcessorProvider.GetProcessor()
		{
			return this;
		}

		// PRIVATE METHODS

		private void CalculateNextPosition(int baseDirection, Vector3 basePosition, float deltaTime, out int nextDirection, out Vector3 positionDelta)
		{
			nextDirection = baseDirection;
			positionDelta = default;

			float remainingDistance = _speed * deltaTime;
			while (remainingDistance > 0.0f)
			{
				Vector3 targetPosition = nextDirection == 0 ? _basePosition + Vector3.up * _currentHeight : _basePosition;
				Vector3 targetDelta    = targetPosition - basePosition;

				if (targetDelta.sqrMagnitude >= (remainingDistance * remainingDistance))
				{
					positionDelta += targetDelta.normalized * remainingDistance;
					break;
				}
				else
				{
					basePosition  += targetDelta;
					positionDelta += targetDelta;

					remainingDistance -= targetDelta.magnitude;

					nextDirection = 1 - nextDirection;
				}
			}
		}

		private void ApplyPositionDelta(Vector3 positionDelta)
		{
			for (int i = 0; i < _entities.Length; ++i)
			{
				ElevatorEntity entity = _entities.Get(i);
				if (entity.Id.IsValid == true)
				{
					NetworkObject networkObject = Runner.FindObject(entity.Id);
					if (networkObject != null)
					{
						KCC kcc = networkObject.GetComponent<KCC>();
						if (kcc.IsProxy == true)
						{
							kcc.Interpolate();
							continue;
						}

						KCCData kccData        = kcc.Data;
						Vector3 targetPosition = kccData.TargetPosition + positionDelta;

						if (_snapVolume.ClosestPoint(targetPosition).AlmostEquals(targetPosition) == true)
						{
							kccData.BasePosition    += positionDelta;
							kccData.DesiredPosition += positionDelta;
							kccData.TargetPosition  += positionDelta;

							kcc.SynchronizeTransform(true, false);
						}
					}
				}
			}
		}

		// DATA STRUCTURES

		protected struct ElevatorEntity : INetworkStruct
		{
			public NetworkId Id;
			public Vector3   Offset;
			public float     SpaceAlpha;
		}
	}
}
