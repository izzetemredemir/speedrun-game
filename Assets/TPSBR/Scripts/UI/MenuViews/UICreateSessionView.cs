using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Fusion;

namespace TPSBR.UI
{
	public class UICreateSessionView : UICloseView
	{
		// PROTECTED MEMBERS

		[SerializeField]
		private UIMapList _maps;
		[SerializeField]
		private UIMap _mapDetail;
		[SerializeField]
		private TMP_InputField _gameName;
		[SerializeField]
		private TMP_Dropdown _gameplay;
		[SerializeField]
		private TMP_InputField _maxPlayers;
		[SerializeField]
		private UIToggle _dedicatedServer;
		[SerializeField]
		private GameObject _dedicatedServerWarning;
		[SerializeField]
		private UIButton _createButton;

		private List<MapSetup> _mapSetups = new List<MapSetup>(8);

		// PRIVATE MEMBERS

		private bool _uiPrepared;

		// UIView INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_maps.UpdateContent += OnUpdateMapContent;
			_maps.SelectionChanged += OnMapSelectionChanged;

			_createButton.onClick.AddListener(OnCreateButton);

			PrepareMapData();
		}

		protected override void OnDeinitialize()
		{
			_maps.UpdateContent -= OnUpdateMapContent;
			_maps.SelectionChanged += OnMapSelectionChanged;

			_createButton.onClick.RemoveListener(OnCreateButton);

			base.OnDeinitialize();
		}

		protected override void OnOpen()
		{
			base.OnOpen();

			if (_uiPrepared == false)
			{
				UpdateDropdowns();

				_maps.Refresh(_mapSetups.Count);
				_maps.Selection = 0;

				OnMapSelectionChanged(0);

				if (_gameName.text.Length < 5)
				{
					_gameName.text = $"{Context.PlayerData.Nickname}'s Game";
				}

				_dedicatedServer.SetIsOnWithoutNotify(false);

				_uiPrepared = true;
			}
		}

		protected override void OnTick()
		{
			base.OnTick();

			_createButton.interactable = CanCreateGame();
			_dedicatedServerWarning.SetActive(_dedicatedServer.isOn);
		}

		// PRIVATE METHODS

		private void OnCreateButton()
		{
			var request = new SessionRequest
			{
				DisplayName  = _gameName.text,
				GameMode     = _dedicatedServer.isOn == true ? GameMode.Server : GameMode.Host,
				GameplayType = (EGameplayType) (_gameplay.value + 1),
				MaxPlayers   = System.Int32.Parse(_maxPlayers.text),
				ScenePath    = _mapSetups[_maps.Selection].ScenePath,
			};
;
			Context.Matchmaking.CreateSession(request);
		}

		private bool CanCreateGame()
		{
			//if (Context.Matchmaking.Connecting == true)
			//	return false;

			if (_maps.Selection < 0)
				return false;

			var mapSetup = _mapSetups[_maps.Selection];

			if (mapSetup == null)
				return false;

			if (System.Int32.TryParse(_maxPlayers.text, out int maxPlayers) == false)
				return false;

			if (maxPlayers < 2 || maxPlayers > mapSetup.MaxPlayers)
				return false;

			if (_gameName.text.Length < 5)
				return false;

			return true;
		}

		private void UpdateDropdowns()
		{
			var options = ListPool.Get<string>(16);

			int defaultOption = 0;
			int i = 0;
			foreach (EGameplayType value in System.Enum.GetValues(typeof(EGameplayType)))
			{
				if (value == EGameplayType.None)
					continue;

				if (value == EGameplayType.BattleRoyale)
				{
					options.Add("Battle Royale");
				}
				else
				{
					options.Add(value.ToString());
				}

				if (value == EGameplayType.Deathmatch)
				{
					defaultOption = i;
				}

				i++;
			}

			_gameplay.ClearOptions();
			_gameplay.AddOptions(options);
			_gameplay.SetValueWithoutNotify(defaultOption);

			ListPool.Return(options);
		}

		private void OnMapSelectionChanged(int index)
		{
			if (index >= 0)
			{
				var mapSetup = _mapSetups[index];

				_mapDetail.SetData(mapSetup);
				_mapDetail.SetActive(true);

				_maxPlayers.text = mapSetup.RecommendedPlayers.ToString();
			}
			else
			{
				_mapDetail.SetActive(false);
			}
		}

		private void OnUpdateMapContent(int index, UIMap content)
		{
			content.SetData(_mapSetups[index]);
		}

		private void PrepareMapData()
		{
			_mapSetups.Clear();

			var allMapSetups = Context.Settings.Map.Maps;

			for (int i = 0; i < allMapSetups.Length; i++)
			{
				var mapSetup = allMapSetups[i];

				if (mapSetup.ShowInMapSelection == true)
				{
					_mapSetups.Add(mapSetup);
				}
			}
		}
	}
}
