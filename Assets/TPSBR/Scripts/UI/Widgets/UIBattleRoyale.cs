using UnityEngine;

namespace TPSBR.UI
{
	public class UIBattleRoyale : UIWidget
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private GameObject _jumpGroup;
		[SerializeField]
		private UIValue _jumpTime;
		[SerializeField]
		private GameObject _waitingForPlayersGroup;
		[SerializeField]
		private UIValue _waitingForPlayersTime;
		[SerializeField]
		private GameObject _waitingForPlayersServerGroup;
		[SerializeField]
		private UIButton _startDropButton;
		[SerializeField]
		private UIButton _addTimeButton;

		private BattleRoyaleGameplayMode _battleRoyale;

		// UIWidget INTERFACE

		protected override void OnInitialize()
		{
			_jumpGroup.SetActive(false);
			_waitingForPlayersGroup.SetActive(false);
			_waitingForPlayersServerGroup.SetActive(false);

			_startDropButton.onClick.AddListener(OnStartDropButton);
			_addTimeButton.onClick.AddListener(OnAddTimeButton);
		}

		protected override void OnDeinitialize()
		{
			_startDropButton.onClick.RemoveListener(OnStartDropButton);
			_addTimeButton.onClick.RemoveListener(OnAddTimeButton);
		}

		protected override void OnVisible()
		{
			_battleRoyale = Context.GameplayMode as BattleRoyaleGameplayMode;
		}

		protected override void OnHidden()
		{
			Context.Input.RequestCursorVisibility(false, ECursorStateSource.Menu);
		}
		protected override void OnTick()
		{
			if (Context.Runner.Exists(_battleRoyale.Object) == false)
				return;

			_waitingForPlayersGroup.SetActive(_battleRoyale.HasStarted == false);

			bool showServerGroup = _battleRoyale.HasStarted == false && (_battleRoyale.Object.HasStateAuthority == true || ApplicationSettings.IsModerator == true);
			_waitingForPlayersServerGroup.SetActive(showServerGroup);

			Context.Input.RequestCursorVisibility(showServerGroup, ECursorStateSource.Menu, false);

			if (_battleRoyale.HasStarted == false)
			{
				_waitingForPlayersTime.SetValue(_battleRoyale.WaitingCooldown);
			}

			bool canJump = _battleRoyale.HasStarted == true && _battleRoyale.AirplaneActive == true && Context.ObservedAgent == null;
			_jumpGroup.SetActive(canJump);

			if (canJump == true)
			{
				_jumpTime.SetValue(_battleRoyale.DropCooldown);
			}

		}

		// PRIVATE METHODS

		private void OnStartDropButton()
		{
			_battleRoyale.StartImmediately();
		}

		private void OnAddTimeButton()
		{
			_battleRoyale.TryAddWaitTime(30f);
		}
	}
}
