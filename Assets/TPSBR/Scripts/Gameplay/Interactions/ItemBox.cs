using System;
using Fusion;
using UnityEngine;

namespace TPSBR
{
	public class ItemBox : NetworkBehaviour, IInteraction
	{
		// HELPERS

		public enum EState
		{
			None,
			Closed,
			Open,
			Locked,
		}

		// PUBLIC MEMBERS

		[Networked(OnChanged = nameof(StateChanged), OnChangedTargets = OnChangedTargets.All), HideInInspector]
		public EState State { get; set; }

		// PRIVATE MEMBERS

		[Header("Item Box")]
		[SerializeField]
		private float _autoCloseTime;
		[SerializeField]
		private float _unlockTime;
		[SerializeField]
		private EState _startState;

		[SerializeField]
		private Transform _lockedState;
		[SerializeField]
		private Transform _unlockedState;

		[SerializeField]
		private AnimationClip _openAnimation;
		[SerializeField]
		private AnimationClip _closeAnimation;

		[Header("Interaction")]
		[SerializeField]
		private string _interactionName;
		[SerializeField]
		private string _interactionDescription;
		[SerializeField]
		private Transform _hudPivot;
		[SerializeField]
		private Collider _interactionCollider;

		[Header("Pickups")]
		[SerializeField]
		private EBehaviour _behaviour;
		[SerializeField]
		private PickupPoint[] _pickupsSetup;

		[Header("Audio")]
		[SerializeField]
		private AudioEffect _audioEffect;
		[SerializeField]
		private AudioSetup _openSound;
		[SerializeField]
		private AudioSetup _closeSound;

		[Networked]
		private TickTimer StateTimer { get; set; }

		private Animation      _animation;
		private StaticPickup[] _nestedPickups;

		// PUBLIC METHODS

		public void Open()
		{
			if (Object.HasStateAuthority == false)
				return;
			if (State != EState.Closed)
				return;

			StateTimer = TickTimer.CreateFromSeconds(Runner, _autoCloseTime);
			State      = EState.Open;

			if (_behaviour == EBehaviour.RandomOnOpen || _nestedPickups[0] == null)
			{
				for (int i = 0; i < _nestedPickups.Length; i++)
				{
					if (_nestedPickups[i] != null)
					{
						Runner.Despawn(_nestedPickups[i].Object);
					}

					_nestedPickups[i] = SpawnPickup(_pickupsSetup[i]);
				}
			}
			else if (_behaviour == EBehaviour.RandomOnSpawn)
			{
				for (int i = 0; i < _nestedPickups.Length; i++)
				{
					_nestedPickups[i].Refresh();
				}
			}
		}

		// IInteraction INTERFACE

		string  IInteraction.Name        => _interactionName;
		string  IInteraction.Description => _interactionDescription;
		Vector3 IInteraction.HUDPosition => _hudPivot != null ? _hudPivot.position : transform.position;
		bool    IInteraction.IsActive    => State == EState.Closed;

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			_animation = GetComponent<Animation>();
		}

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			if (Object.HasStateAuthority == false)
			{
				OnStateChanged(State);

				if (ApplicationSettings.IsStrippedBatch == true)
				{
					gameObject.SetActive(false);
				}

				return;
			}

			_nestedPickups = new StaticPickup[_pickupsSetup.Length];

			switch (_startState)
			{
				case EState.None:
				case EState.Closed:
					Unlock();
					break;
				case EState.Open:
					Open();
					break;
				case EState.Locked:
					Lock();
					break;
				default:
					break;
			}
		}

		public override void FixedUpdateNetwork()
		{
			if (Object.HasStateAuthority == false)
				return;

			switch (State)
			{
				case EState.Open:   Update_Open();   break;
				case EState.Locked: Update_Locked(); break;
			}
		}

		// PRIVATE METHODS

		private void Update_Open()
		{
			if (StateTimer.Expired(Runner) == false)
				return;

			Lock();
		}

		private void Update_Locked()
		{
			if (StateTimer.Expired(Runner) == false)
				return;

			Unlock();
		}

		private void Lock()
		{
			if (State == EState.Locked)
				return;

			for (int i = 0; i < _nestedPickups.Length; i++)
			{
				if (_nestedPickups[i] != null)
				{
					_nestedPickups[i].SetIsDisabled(true);
				}
			}

			StateTimer = TickTimer.CreateFromSeconds(Runner, _unlockTime);
			State      = EState.Locked;
		}

		private void Unlock()
		{
			State = EState.Closed;

			for (int i = 0; i < _nestedPickups.Length; i++)
			{
				if (_nestedPickups[i] != null)
				{
					_nestedPickups[i].SetIsDisabled(true);
				}
			}
		}

		private StaticPickup SpawnPickup(PickupPoint point)
		{
			var prefab = ChoosePickup(point.Pickups);

			var pickup = Runner.Spawn(prefab, point.Transform.position, point.Transform.rotation);
			pickup.SetBehaviour(StaticPickup.EBehaviour.Interaction, -1f);
			pickup.PickupConsumed += OnPickupConsumed;

			return pickup;
		}

		private void OnStateChanged(EState state)
		{
			if (ApplicationSettings.IsStrippedBatch == true)
				return;

			_lockedState.SetActive(state == EState.Locked);
			_unlockedState.SetActive(state != EState.Locked);
			_interactionCollider.enabled = state == EState.Closed;

			switch (state)
			{
				case EState.Open:
					if (_animation.clip != _openAnimation)
					{
						_animation.clip = _openAnimation;
						_animation.Play();

						_audioEffect.Play(_openSound, EForceBehaviour.ForceAny);
					}
					break;
				case EState.Closed:
				case EState.Locked:
					if (_animation.clip != _closeAnimation)
					{
						_animation.clip = _closeAnimation;
						_animation.Play();

						_audioEffect.Play(_closeSound, EForceBehaviour.ForceAny);
					}
					break;
				default:
					break;
			}
		}

		private StaticPickup ChoosePickup(PickupData[] data)
		{
			int totalProbability = 0;

			for (int i = 0; i < data.Length; i++)
			{
				totalProbability += data[i].Probability;
			}

			if (totalProbability <= 0)
				return null;

			int targetProbability = UnityEngine.Random.Range(0, totalProbability);
			int currentProbability = 0;

			for (int i = 0; i < data.Length; i++)
			{
				currentProbability += data[i].Probability;

				if (targetProbability < currentProbability)
				{
					return data[i].Pickup;
				}
			}

			return null;
		}

		private void OnPickupConsumed(StaticPickup pickup)
		{
			if (pickup.AutoDespawn == false)
				return;

			int index = Array.IndexOf(_nestedPickups, pickup);
			if (index >= 0)
			{
				// Clear reference, pickup will be auto despawned
				_nestedPickups[index] = null;
			}
		}

		private static void StateChanged(Changed<ItemBox> changed)
		{
			changed.Behaviour.OnStateChanged(changed.Behaviour.State);
		}

		// HELPERS

		private enum EBehaviour
		{
			RandomOnSpawn,
			RandomOnOpen,
		}

		[Serializable]
		private class PickupPoint
		{
			public Transform      Transform;
			public PickupData[]   Pickups;
		}

		[Serializable]
		private class PickupData
		{
			public StaticPickup Pickup;
			public int          Probability = 1;
		}
	}
}
