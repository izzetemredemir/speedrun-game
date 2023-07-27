using System;
using UnityEngine;
using DG.Tweening;

namespace TPSBR
{
	public class SimpleAnimation : CoreBehaviour
	{
		// PUBLIC MEMBERS

		public float StartDelay;

		public Animation Translation;
		public Animation Rotation;
		public Animation Scale;

		[Space]
		public bool PlayAutomatically = true;
		public bool ResetOnStop = true;

		public bool IsPlaying => Translation.HasStarted == true || Rotation.HasStarted == true || Scale.HasStarted;

		// PRIVATE MEMBERS

		private bool _hasStarted;
		private float _delay;

		private bool _playReversed;

		// PUBLIC METHODS

		public void Play(float delay, bool forceRestart = false)
		{
			if (IsPlaying == true && forceRestart == false)
				return;

			Stop();

			_playReversed = false;

			Translation.TryStart(transform.localPosition);
			Rotation.TryStart(transform.localRotation.eulerAngles);
			Scale.TryStart(transform.localScale);

			_delay = delay;
		}

		public void PlayReversed(float delay)
		{
			Stop();

			_playReversed = true;

			Translation.TryStart(transform.localPosition);
			Rotation.TryStart(transform.localRotation.eulerAngles);
			Scale.TryStart(transform.localScale);

			_delay = delay;
		}

		public void Stop()
		{
			if (ResetOnStop == true)
			{
				if (Translation.HasStarted == true)
				{
					transform.localPosition = Translation.RestartValue;
				}

				if (Rotation.HasStarted == true)
				{
					transform.localRotation = Quaternion.Euler(Rotation.RestartValue);
				}

				if (Scale.HasStarted == true)
				{
					transform.localScale = Scale.RestartValue;
				}

				_playReversed = false;
			}

			Translation.Stop();
			Rotation.Stop();
			Scale.Stop();
		}

		// MONOBEHAVIOR

		protected void OnEnable()
		{
			if (PlayAutomatically == true)
			{
				_delay = StartDelay;
			}
		}

		protected void Update()
		{
			_delay -= Time.deltaTime;
			if (_delay > 0)
				return;
			
			if (_hasStarted == false && PlayAutomatically == true)
			{
				_hasStarted = true;
				Play(0f);
			}

			if (Translation.CanUpdate == true)
			{
				transform.localPosition = Translation.Update(_playReversed);
			}

			if (Rotation.CanUpdate == true)
			{
				transform.localRotation = Quaternion.Euler(Rotation.Update(_playReversed));
			}

			if (Scale.CanUpdate == true)
			{
				transform.localScale = Scale.Update(_playReversed);
			}
		}

		protected void OnDisable()
		{
			Stop();
			_hasStarted = false;
			_delay = 0f;
		}

		// HELPERS

		[Serializable]
		public class Animation
		{
			// PUBLIC MEMBERS

			public Vector3       Value;
			public Ease          Ease = Ease.Linear;
			public float         Duration;
			public float         Delay;
			public EPlayBehavior Behavior = EPlayBehavior.Once;
			public bool          ValueIsAbsolute;
			public EDirection    Direction = EDirection.FromTransformValues;

			public bool          HasStarted => _hasStarted;
			public bool          CanUpdate => _hasStarted == true && _isFinished == false;
			public Vector3       RestartValue => _restartValue;

			// PRIVATE MEMBERS

			private bool _hasStarted;
			private bool _isFinished;
			private Vector3 _restartValue;
			private float _currentTime;

			private Vector3 _start;
			private Vector3 _target;

			// PUBLIC METHODS

			public void TryStart(Vector3 initialValue)
			{
				_hasStarted = Behavior != EPlayBehavior.None && Value != Vector3.zero && Duration > 0;

				if (_hasStarted == false)
					return;

				if (Direction == EDirection.FromTransformValues)
				{
					_restartValue = _start = initialValue;
					_target = ValueIsAbsolute == true ? Value : _start + Value;
				}
				else
				{
					_restartValue = initialValue;
					_start = ValueIsAbsolute == true ? Value : initialValue - Value;
					_target = initialValue;
				}

				_currentTime = -Delay;
				_isFinished = false;
			}

			public void Stop()
			{
				_hasStarted = false;
				_isFinished = false;
			}

			public Vector3 Update(bool reversed)
			{
				_currentTime += Time.deltaTime;

				if (_currentTime >= Duration)
				{
					if (Behavior == EPlayBehavior.Once)
					{
						_isFinished = true;
						return reversed == true ? _start : _target;
					}
					else if (Behavior == EPlayBehavior.Restart)
					{
						_currentTime = _currentTime - Duration - Delay;
					}
					else if (Behavior == EPlayBehavior.PingPong)
					{
						var previousStart = _start;
						_start = _target;
						_target = previousStart;
						_currentTime = _currentTime - Duration - Delay;
					}
					else if (Behavior == EPlayBehavior.Continue)
					{
						var delta = _target - _start;
						_start = _target;
						_target = _target + delta;
						_currentTime = _currentTime - Duration - Delay;
					}
				}

				float linearProgress = reversed == true ? 1f - _currentTime / Duration : _currentTime / Duration;
				float progress = DOVirtual.EasedValue(0f, 1f, linearProgress, Ease);
				return Vector3.Lerp(_start, _target, progress);
			}
		}

		public enum EPlayBehavior
		{
			None,
			Once,
			Restart,
			PingPong,
			Continue,
		}

		public enum EDirection
		{
			FromTransformValues,
			ToTransformValues,
		}
	}
}
