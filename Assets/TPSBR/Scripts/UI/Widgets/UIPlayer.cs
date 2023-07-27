using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
	public class UIPlayer : UIBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private TextMeshProUGUI _playerName;
		[SerializeField]
		private Image _playerIcon;

		// PUBLIC MEMBERS

		public void SetData(SceneContext context, IPlayer player)
		{
			_playerName.text = player.Nickname;

			if (_playerIcon != null)
			{
				var agentSetup = context.Settings.Agent.GetAgentSetup(player.AgentPrefabID);
				Sprite sprite = agentSetup != null ? agentSetup.Icon : null;

				_playerIcon.sprite = sprite;
				_playerIcon.SetActive(sprite != null);
			}
		}
	}
}
