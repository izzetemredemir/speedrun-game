using DG.Tweening;
using UnityEngine;

namespace TPSBR.UI
{
	[RequireComponent(typeof(CanvasGroup))]
	public class UIFader : UIBehaviour
	{
		// PUBLIC MEMBERS

		public bool           IsFinished     => _isFinished;

		public float          StartDelay;
		public EFadeDirection Direction      = EFadeDirection.FadeIn;
		public float          Duration       = 0.5f;
		public Ease           Ease           = Ease.OutQuad;
		public EPlayBehavior  Behaviour      = EPlayBehavior.Once;
		public bool           ResetOnDisable = true;
		public float          FadeOutValue   = 0f;

		// PRIVATE MEMBERS

		private float _resetValue;
		private float _startValue;
		private float _targetValue;
		private float _time;
		private bool _isFinished;

		// MONOBEHAVIOR

		protected void OnEnable()
		{
			if (_isFinished == false)
			{
				_resetValue = CanvasGroup.alpha;
				_startValue = Direction == EFadeDirection.FadeIn ? FadeOutValue : _resetValue;
				_targetValue = Direction == EFadeDirection.FadeIn ? _resetValue : FadeOutValue;
				_time = -StartDelay;

				CanvasGroup.alpha = _startValue;
			}
		}

		protected void Update()
		{
			if (_isFinished == true)
			{
				if (Behaviour == EPlayBehavior.PingPong)
				{
					float previousStart = _startValue;

					_startValue = _targetValue;
					_targetValue = previousStart;
					_time = 0f;
					_isFinished = false;
				}
				else if (Behaviour == EPlayBehavior.Restart)
				{
					_time = 0f;
					_isFinished = false;
				}
				else
				{
					return;
				}
			}

			_time += Time.deltaTime;
			
			if (_time <= 0f)
				return;
			
			if (_time >= Duration)
			{
				_time = Duration;
				_isFinished = true;
			}

			CanvasGroup.alpha = DOVirtual.EasedValue(_startValue, _targetValue, _time / Duration, Ease);
		}

		protected void OnDisable()
		{
			if (ResetOnDisable == true)
			{
				CanvasGroup.alpha = _resetValue;
			}
			
			_isFinished = false;
		}

		// HELPERS

		public enum EFadeDirection
		{
			FadeIn,
			FadeOut,
		}

		public enum EPlayBehavior
		{
			//None,
			Once = 1,
			Restart = 2,
			PingPong = 3,
		}
	}
}
