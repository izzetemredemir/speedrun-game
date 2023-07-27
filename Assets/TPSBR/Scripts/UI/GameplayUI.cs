using System.Collections;
using UnityEngine;

namespace TPSBR.UI
{
	public class GameplayUI : SceneUI
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private float _gameOverScreenDelay = 3f;
		
		private UIDeathView _deathView;

		private bool _gameOverShown;
		private Coroutine _gameOverCoroutine;

		// PUBLIC METHODS

		public void RefreshCursorVisibility()
		{
			bool showCursor = false;

			for (int i = 0; i < _views.Length; i++)
			{
				var view = _views[i];

				if (view.IsOpen == true && view.NeedsCursor == true)
				{
					showCursor = true;
					break;
				}
			}

			Context.Input.RequestCursorVisibility(showCursor, ECursorStateSource.UI);
		}

		// SceneUI INTERFACE

		protected override void OnInitializeInternal()
		{
			base.OnInitializeInternal();

			_deathView = Get<UIDeathView>();
		}

		protected override void OnActivate()
		{
			base.OnActivate();

			if (Context.Runner.Mode == Fusion.SimulationModes.Server)
			{
				Open<UIDedicatedServerView>();
			}
		}

		protected override void OnDeactivate()
		{
			base.OnDeactivate();
			
			if (_gameOverCoroutine != null)
			{
				StopCoroutine(_gameOverCoroutine);
				_gameOverCoroutine = null;
			}
			
			_gameOverShown = false;
		}

		protected override void OnTickInternal()
		{
			base.OnTickInternal();

			if (_gameOverShown == true)
				return;
			if (Context.Runner == null || Context.Runner.Exists(Context.GameplayMode.Object) == false)
				return;

			var player = Context.NetworkGame.GetPlayer(Context.LocalPlayerRef);
			if (player == null || player.Statistics.IsAlive == true)
			{
				_deathView.Close();
			}
			else
			{
				_deathView.Open();
			}

			if (Context.GameplayMode.State == GameplayMode.EState.Finished && _gameOverCoroutine == null)
			{
				_gameOverCoroutine = StartCoroutine(ShowGameOver_Coroutine(_gameOverScreenDelay));
			}
		}

		protected override void OnViewOpened(UIView view)
		{
			RefreshCursorVisibility();
		}

		protected override void OnViewClosed(UIView view)
		{
			RefreshCursorVisibility();
		}
		
		// PRIVATE METHODS
		
		private IEnumerator ShowGameOver_Coroutine(float delay)
		{
			yield return new WaitForSeconds(delay);
			
			_gameOverShown = true;
			
			_deathView.Close();
			Close<UIGameplayView>();
			Close<UIScoreboardView>();
			Close<UIGameplayMenu>();
			Close<UIAnnouncementsView>();

			Open<UIGameOverView>();

			_gameOverCoroutine = null;
		}
	}
}
