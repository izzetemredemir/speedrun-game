using System;
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

namespace TPSBR
{
	using Random = UnityEngine.Random;

	[Serializable]
	public class ShakeSetup
	{
		public float        Duration  = 0.5f;
		public float        Magnitude = 0.05f;
		public float        Frequency = 10f;
		public float        FadeIn    = 0.1f;
		public float        FadeOut   = 0.2f;
		public Ease         Ease      = Ease.Linear;
		public Vector3      Axis      = new Vector3(1f, 1f, 1f);
		public EShakeTarget Target    = EShakeTarget.Position;
	}

	public enum EShakeTarget
	{
		None,
		Position,
		Rotation,
	}

	public enum EShakeForce
	{
		None,
		ReplaceSame,
		Add,
	}

	public class ShakeEffect : CoreBehaviour
	{
		// PUBLIC MEMBERS

		public bool IsPlaying => _activeShakes.Count > 0f;

		// PRIVATE MEMBERS

		[SerializeField]
		private ShakeSetup _defaultSetup;

		private List<ShakeData> _activeShakes = new List<ShakeData>(32);

		private Vector3 _defaultPosition;
		private Quaternion _defaultRotation;

		// PUBLIC MEMBERS

		public void Play(ShakeSetup setup, EShakeForce force = EShakeForce.Add)
		{
			if (setup == null || setup.Target == EShakeTarget.None || setup.Duration <= 0f || setup.Magnitude <= 0f)
				return;

			if (IsPlaying == false || force == EShakeForce.Add)
			{
				AddShake(setup);
			}
			else if (force == EShakeForce.ReplaceSame)
			{
				for (int i = 0; i < _activeShakes.Count; i++)
				{
					var shake = _activeShakes[i];

					if (shake.Setup == setup)
					{
						shake.Cooldown = Mathf.Max(setup.Duration - setup.FadeIn, shake.Cooldown);
						return;
					}
				}

				AddShake(setup);
			}
		}

		public void Play(EShakeForce force = EShakeForce.Add)
		{
			Play(_defaultSetup, force);
		}

		public void Stop(ShakeSetup setup, bool immediate = false)
		{
			if (IsPlaying == false)
				return;

			for (int i = 0; i < _activeShakes.Count; i++)
			{
				var shake = _activeShakes[i];

				if (shake.Setup != setup)
					continue;

				if (immediate == true || shake.Setup.FadeOut <= 0f)
				{
					RemoveShake(i);
					return;
				}

				shake.Cooldown = Mathf.Min(shake.Cooldown, shake.Setup.FadeOut);
			}
		}

		public void Stop(bool immediate = false)
		{
			if (IsPlaying == false)
				return;

			for (int i = _activeShakes.Count - 1; i >= 0; i--)
			{
				Stop(_activeShakes[i].Setup, immediate);
			}
		}

		// MONOBEHAVIOUR

		protected void Awake()
		{
			_defaultPosition = transform.localPosition;
			_defaultRotation = transform.localRotation;
		}

		protected void Update()
		{
			if (IsPlaying == false)
			{
				transform.localPosition = _defaultPosition;
				transform.localRotation = _defaultRotation;
				return;
			}

			var positionOffset = Vector3.zero;
			var rotationOffset = Vector3.zero;

			for (int i = 0; i < _activeShakes.Count; i++)
			{
				var shake = _activeShakes[i];

				if (shake.Setup.Target == EShakeTarget.Position)
				{
					positionOffset += shake.GetOffset(Time.deltaTime);
				}
				else
				{
					rotationOffset += shake.GetOffset(Time.deltaTime);
				}
			}

			transform.localPosition = _defaultPosition + positionOffset;
			transform.localRotation = _defaultRotation * Quaternion.Euler(rotationOffset);

			for (int i = _activeShakes.Count - 1; i >= 0; i--)
			{
				if (_activeShakes[i].IsFinished == true)
				{
					RemoveShake(i);
				}
			}
		}

		// PRIVATE METHODS

		private void AddShake(ShakeSetup setup)
		{
			var shake = Pool.Get<ShakeData>();

			shake.Reset(setup);
			_activeShakes.Add(shake);
		}

		private void RemoveShake(int index)
		{
			var shakeData = _activeShakes[index];
			_activeShakes.RemoveAt(index);

			Pool.Return(shakeData);
		}

		// HELPERS

		private class ShakeData
		{
			public ShakeSetup Setup;
			public float      Cooldown;

			public bool       IsFinished => Cooldown <= 0f;


			[NonSerialized]
			private float     _elapsedTime;
			[NonSerialized]
			private float     _normalChangeDuration;
			[NonSerialized]
			private float     _changeDuration;

			[NonSerialized]
			private Vector3   _startPosition;
			[NonSerialized]
			private Vector3   _targetPosition;
			[NonSerialized]
			private float     _changeCooldown;
			[NonSerialized]
			private Vector3   _lastOffset;

			private float     _changeDurationMultiplier;

			public void Reset(ShakeSetup setup)
			{
				Setup = setup;
				Cooldown = setup.Duration;

				_elapsedTime = 0f;

				_normalChangeDuration = 1f / setup.Frequency;
				_changeDuration = _normalChangeDuration;
				_changeCooldown = 0f;

				_startPosition = Vector3.zero;
				_targetPosition = Vector3.zero;
				_lastOffset = Vector3.zero;
			}

			public Vector3 GetOffset(float deltaTime)
			{
				bool isStart = _elapsedTime == 0f;
				bool wasEnd = Cooldown <= _normalChangeDuration * 0.5f;

				_elapsedTime += deltaTime;

				Cooldown -= deltaTime;
				_changeCooldown -= deltaTime;

				bool isEnd = wasEnd == false && Cooldown <= _normalChangeDuration * 0.5f;

				if (_changeCooldown <= 0f || isEnd == true)
				{
					float magnitudeProgress = 1f;

					if (Setup.FadeIn > 0f && _elapsedTime < Setup.FadeIn)
					{
						magnitudeProgress = _elapsedTime / Setup.FadeIn;
					}
					else if (Setup.FadeOut > 0f && Cooldown < Setup.FadeOut)
					{
						magnitudeProgress = Cooldown / Setup.FadeOut;
					}

					float magnitude = Setup.Magnitude * magnitudeProgress;

					// Recalculate change duration in case frequency changed
					_normalChangeDuration = 1f / Setup.Frequency;

					if (isEnd == true)
					{
						_startPosition = _lastOffset;
						_targetPosition = Vector3.zero;

						_changeDuration = Cooldown + Time.deltaTime;
						_changeCooldown = Cooldown;
					}
					else if (isStart == true)
					{
						_startPosition = Vector3.zero;
						_targetPosition = Vector3.Scale(Random.onUnitSphere, Setup.Axis).normalized * magnitude;

						// We are covering only half of shake distance on start
						_changeDuration = _normalChangeDuration * 0.5f;
						_changeCooldown += _changeDuration;
					}
					else
					{
						_startPosition = _targetPosition;

						var randomRotation = Quaternion.Euler(Random.Range(-60, 60), Random.Range(-60, 60), Random.Range(-60, 60));
						_targetPosition = Vector3.Scale(randomRotation * -_targetPosition, Setup.Axis).normalized * magnitude;

						_changeDuration = _normalChangeDuration;
						_changeCooldown += _changeDuration;
					}
				}

				float progress = 1 - _changeCooldown / _changeDuration;
				_lastOffset = Vector3.Lerp(_startPosition, _targetPosition, DOVirtual.EasedValue(0f, 1f, progress, Setup.Ease));

				return _lastOffset;
			}
		}
	}
}
