using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TPSBR.UI
{
	public class UIMap : UIBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private TextMeshProUGUI _displayName;
		[SerializeField]
		private TextMeshProUGUI _description;
		[SerializeField]
		private Image _image;
		[SerializeField]
		private TextMeshProUGUI _recommendedPlayers;
		[SerializeField]
		private string _recommendedPlayersFormat;

		// PUBLIC MEMBERS

		public void SetData(MapSetup setup)
		{
			_displayName.SetTextSafe(setup.DisplayName);
			_description.SetTextSafe(setup.Description);

			if (_image != null)
			{
				_image.sprite = setup.Image;
			}

			if (_recommendedPlayers != null)
			{
				_recommendedPlayers.text = _recommendedPlayersFormat.HasValue() == true ? string.Format(_recommendedPlayersFormat, setup.RecommendedPlayers) : setup.RecommendedPlayers.ToString();
			}
		}
	}
}
