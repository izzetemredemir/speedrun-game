using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

namespace TPSBR.UI
{
	public class UISession : UIBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private TextMeshProUGUI _name;
		[SerializeField]
		private TextMeshProUGUI _playerCount;
		[SerializeField]
		private TextMeshProUGUI _map;
		[SerializeField]
		private Image _mapImage;
		[SerializeField]
		private TextMeshProUGUI _gameplayType;
		[SerializeField]
		private TextMeshProUGUI _state;
		[SerializeField]
		private string _emptyField = "-";

		// PUBLIC METHODS

		public void SetData(SessionInfo sessionInfo)
		{
			if (sessionInfo == null)
				return;

			int playerCount = sessionInfo.PlayerCount;
			int maxPlayers  = sessionInfo.MaxPlayers;

			if (playerCount > 0 && sessionInfo.GetGameMode() == GameMode.Server)
			{
				playerCount -= 1;
				maxPlayers  -= 1;
			}

			_name.text = sessionInfo.GetDisplayName();
			_playerCount.text = $"{playerCount}/{maxPlayers}";

			var mapSetup = sessionInfo.GetMapSetup();
			_map.text = mapSetup != null ? mapSetup.DisplayName : _emptyField;

			if (mapSetup != null && _mapImage != null)
			{
				_mapImage.sprite = mapSetup.Image;
			}

			var gameplayType = sessionInfo.GetGameplayType();
			_gameplayType.text = gameplayType != EGameplayType.None ? gameplayType.ToString() : _emptyField;

			// We do not have lobby state for now
			_state.text = sessionInfo.IsOpen == true ? "In Game" : "Finished";
		}
	}
}
