namespace TPSBR.UI
{
	using UnityEngine;
	using UnityEngine.InputSystem;

	public class UIScoreboardView : UIView
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private UIGameInfo  _gameInfo;
		[SerializeField]
		private CanvasGroup _fader;
		[SerializeField]
		private float       _fadeSpeed = 5f;

		private UIScoreboard _board;
		private float _targetAlpha;

		// PUBLIC METHODS

		public void Show()
		{
			_board.SetActive(true);
			_targetAlpha = 1f;

			if (Context.Runner != null)
			{
				_gameInfo.UpdateInfo(Context.Runner, true);
			}
		}

		public void Hide(bool immediately = false)
		{
			_targetAlpha = 0f;
			_board.SetActive(false);

			if (immediately == true)
			{
				_fader.alpha = _targetAlpha;
			}
		}

		// UIView INTERFAFCE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_board = GetComponentInChildren<UIScoreboard>(true);
		}

		protected override void OnOpen()
		{
			base.OnOpen();

			Hide(true);
		}

		protected override void OnTick()
		{
			base.OnTick();

			if (Keyboard.current.tabKey.isPressed == true && IsTopView(true) == true)
			{
				Show();
			}
			else
			{
				Hide();
			}

			_fader.alpha = Mathf.Lerp(_fader.alpha, _targetAlpha, Time.deltaTime * _fadeSpeed);

			if (_targetAlpha <= 0.0f || Context.Runner == null)
				return;

			_gameInfo.UpdateInfo(Context.Runner);
		}
	}
}
