using Fusion;
using System.Collections.Generic;
using Fusion.Photon.Realtime;
using UnityEngine;
using TMPro;
using Unity.Services.Matchmaker.Models;

#pragma warning disable 4014

namespace TPSBR.UI
{
	public class UIMultiplayerView : UICloseView
	{
		// PRIVATE MEMBERS

		[SerializeField] private UISession _sessionDetail;
		[SerializeField] private UIButton _createSessionButton;
		[SerializeField] private UIButton _quickPlayButton;
		[SerializeField] private UIButton _cancelQuickPlayButton;
		[SerializeField] private UIButton _joinButton;
		[SerializeField] private UIButton _settingsButton;
		[SerializeField] private TMP_Dropdown _regionDropdown;

		[SerializeField] private UIBehaviour _refreshingGroup;
		[SerializeField] private UIBehaviour _noSessionsGroup;
		[SerializeField] private TextMeshProUGUI _errorText;

		[SerializeField] private TextMeshProUGUI _applicationVersion;

		private UISessionList _sessionList;
		private UIMatchmakerView _matchmakerView;

		private List<SessionInfo> _sessionInfo = new List<SessionInfo>(32);
		private SessionInfo _selectedSession;

		private SimpleAnimation _joinButtonAnimation;

		// PUBLIC METHODS

		public void StartQuickPlay()
		{
			OnQuickPlayButton();
		}

		// UIView INTEFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_sessionList = GetComponentInChildren<UISessionList>();
			_sessionList.SelectionChanged += OnSessionSelectionChanged;
			_sessionList.UpdateContent += OnUpdateSessionListContent;

			_createSessionButton.onClick.AddListener(OnCreateGameButton);
			_quickPlayButton.onClick.AddListener(OnQuickPlayButton);
			_cancelQuickPlayButton.onClick.AddListener(OnCancelQuickPlay);
			_joinButton.onClick.AddListener(OnJoinButton);
			_settingsButton.onClick.AddListener(OnSettingsButton);
			_regionDropdown.onValueChanged.AddListener(OnRegionChanged);

			_sessionDetail.SetActive(false);

			_applicationVersion.text = $"Version {Application.version}";

			_joinButtonAnimation = _joinButton.GetComponent<SimpleAnimation>();

			PrepareRegionDropdown();
		}

		protected override void OnDeinitialize()
		{
			_sessionList.SelectionChanged -= OnSessionSelectionChanged;
			_sessionList.UpdateContent -= OnUpdateSessionListContent;

			_createSessionButton.onClick.RemoveListener(OnCreateGameButton);
			_quickPlayButton.onClick.RemoveListener(OnQuickPlayButton);
			_cancelQuickPlayButton.onClick.RemoveListener(OnCancelQuickPlay);
			_joinButton.onClick.RemoveListener(OnJoinButton);
			_settingsButton.onClick.RemoveListener(OnSettingsButton);
			_regionDropdown.onValueChanged.RemoveListener(OnRegionChanged);

			base.OnDeinitialize();
		}

		protected override void OnOpen()
		{
			base.OnOpen();

			Context.Matchmaking.SessionListUpdated += OnSessionListUpdated;
			Context.Matchmaking.LobbyJoined += OnLobbyJoined;
			Context.Matchmaking.LobbyJoinFailed += OnLobbyJoinFailed;
			Context.Matchmaking.LobbyLeft += OnLobbyLeft;

			Context.Matchmaker.MatchFound += OnMatchFound;
			Context.Matchmaker.MatchmakerFailed += OnMatchmakerFailed;

			OnLobbyLeft();

			TryJoinLobby(true);

			var currentRegion = Context.RuntimeSettings.Region;
			int regionIndex = System.Array.FindIndex(Context.Settings.Network.Regions, t => t.Region == currentRegion);
			_regionDropdown.SetValueWithoutNotify(regionIndex);
		}

		protected override void OnClose()
		{
			Context.Matchmaking.SessionListUpdated -= OnSessionListUpdated;
			Context.Matchmaking.LobbyJoined -= OnLobbyJoined;
			Context.Matchmaking.LobbyJoinFailed -= OnLobbyJoinFailed;
			Context.Matchmaking.LobbyLeft -= OnLobbyLeft;

			Context.Matchmaker.MatchFound -= OnMatchFound;
			Context.Matchmaker.MatchmakerFailed -= OnMatchmakerFailed;

			Context.Matchmaking.LeaveLobby();

			base.OnClose();
		}

		protected override void OnTick()
		{
			base.OnTick();

			_refreshingGroup.SetActive(Context.Matchmaking.IsJoiningToLobby);

			bool canJoin = CanJoinSession(_selectedSession);
			_joinButton.interactable = canJoin;
			_joinButtonAnimation.enabled = canJoin;
		}

		// PRIVATE METHODS

		private void TryJoinLobby(bool force)
		{
			if (PhotonAppSettings.Instance.AppSettings.AppIdFusion.HasValue() == true)
			{
				Context.Matchmaking.JoinLobby(force);
			}
			else
			{
				var errorDialog = Open<UIErrorDialogView>();

				errorDialog.Title.text = "Missing App Id";
				errorDialog.Description.text = "Fusion App Id is not assigned in the Photon App Settings asset.\n\nPlease follow instructions in the Fusion BR documentation on how to create and assign App Id.";

				errorDialog.HasClosed += () =>
				{
				#if UNITY_EDITOR
					UnityEditor.Selection.activeObject = PhotonAppSettings.Instance;
					UnityEditor.EditorGUIUtility.PingObject(PhotonAppSettings.Instance);
				#endif

					Close();
				};
			}
		}

		private void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionInfo)
		{
			string selectedSessionName = _selectedSession != null ? _selectedSession.Name : string.Empty;

			_sessionInfo.Clear();

			for (int i = 0; i < sessionInfo.Count; i++)
			{
				var session = sessionInfo[i];

				// Do not show invalid sessions
				if (session.IsValid == false || session.IsOpen == false || session.IsVisible == false)
					continue;

				_sessionInfo.Add(session);
			}

			_sessionList.Refresh(_sessionInfo.Count);
			_sessionList.Selection = _sessionInfo.FindIndex(t => t.Name == selectedSessionName);

			_noSessionsGroup.SetActive(_sessionInfo.Count == 0);

			_selectedSession = _sessionList.Selection >= 0 ? _sessionInfo[_sessionList.Selection] : null;

			UpdateSessionDetail();
		}

		private void OnLobbyJoined()
		{
			_errorText.text = string.Empty;
		}

		private void OnLobbyJoinFailed(string region)
		{
			var regionInfo = Context.Settings.Network.GetRegionInfo(region);

			var regionText = regionInfo != null ? $"{regionInfo.DisplayName} ({regionInfo.Region})" : "Unknown";
			_errorText.text = $"Joining lobby in region {regionText} failed";
		}

		private void OnLobbyLeft()
		{
			_errorText.text = string.Empty;
			_noSessionsGroup.SetActive(false);
			ClearSessions();
		}

		private void ClearSessions()
		{
			_sessionList.Clear();
			_sessionInfo.Clear();
			_selectedSession = null;

			_sessionDetail.SetActive(false);
		}

		private void OnCreateGameButton()
		{
			if (ApplicationSettings.IsPublicBuild == true && ApplicationSettings.IsModerator == false)
			{
				UIJokeDialogView jokeDialog = Open<UIJokeDialogView>();
				jokeDialog.Title.text = "Moderators Only";
				jokeDialog.Description.text = "You are not allowed to create a game in public build.";
				jokeDialog.JokeButton01Text.text = "I'm moderator!";
				jokeDialog.JokeButton02Text.text = "I'm moderator!";
			}
			else
			{
				Open<UICreateSessionView>();
			}
		}

		private async void OnQuickPlayButton()
		{
			if (Context.PlayerData.UnityID.HasValue() == true)
			{
				_errorText.text = string.Empty;
				_matchmakerView = Open<UIMatchmakerView>();
				await Context.Matchmaker.StartMatchmaker(Global.Settings.Network.GetCustomOrDefaultQueueName());
			}
			else
			{
				var infoDialog = Open<UIInfoDialogView>();

				infoDialog.Title.text = "Unity Gaming Services";
				infoDialog.Description.text = "For matchmaking functionality Unity Gaming Services need to be configured.\n\nPlease follow instructions in the Fusion BR documentation on how to add Multiplay support.";
			}
		}

		private async void OnCancelQuickPlay()
		{
			await Context.Matchmaker.CancelMatchmaker();
		}

		private void OnMatchFound(MultiplayAssignment assignment)
		{
			Context.Matchmaking.JoinSession("mm-" + assignment.MatchId);
			if (_matchmakerView != null)
			{
				_matchmakerView.Close();
				_matchmakerView = null;
			}
		}

		private void OnMatchmakerFailed(string message)
		{
			_errorText.text = message;
			if (_matchmakerView != null)
			{
				_matchmakerView.Close();
				_matchmakerView = null;
			}
		}

		private void OnSessionSelectionChanged(int index)
		{
			_selectedSession = index >= 0 ? _sessionInfo[index] : null;
			UpdateSessionDetail();
		}

		private void OnUpdateSessionListContent(int index, UISession content)
		{
			content.SetData(_sessionInfo[index]);
		}

		private void OnJoinButton()
		{
			Context.Matchmaking.JoinSession(_selectedSession);
		}

		private void OnSettingsButton()
		{
			Open<UISettingsView>();
		}

		private void OnRegionChanged(int regionIndex)
		{
			var region = Context.Settings.Network.Regions[regionIndex].Region;
			Context.RuntimeSettings.Region = region;

			TryJoinLobby(false);
		}

		private void UpdateSessionDetail()
		{
			if (_selectedSession == null)
			{
				_sessionDetail.SetActive(false);
			}
			else
			{
				_sessionDetail.SetActive(true);
				_sessionDetail.SetData(_selectedSession);

				_joinButton.interactable = CanJoinSession(_selectedSession);
			}
		}

		private bool CanJoinSession(SessionInfo session)
		{
			if (session == null)
				return false;

			if (session.PlayerCount >= session.MaxPlayers)
				return false;

			if (session.IsOpen == false || session.IsVisible == false)
				return false;

			if (session.HasMap() == false)
				return false;

			return true;
		}

		private void PrepareRegionDropdown()
		{
			var options = ListPool.Get<TMP_Dropdown.OptionData>(16);
			var regions = Context.Settings.Network.Regions;

			for (int i = 0; i < regions.Length; i++)
			{
				var regionInfo = regions[i];
				options.Add(new TMP_Dropdown.OptionData(regionInfo.DisplayName, regionInfo.Icon));
			}

			_regionDropdown.ClearOptions();
			_regionDropdown.AddOptions(options);

			ListPool.Return(options);
		}
	}
}
