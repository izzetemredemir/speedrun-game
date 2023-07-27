using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace TPSBR.UI
{
	public class UIKillFeedItem : UIBehaviour
	{
		// PUBLIC MEMBERS

		public float VisibilityTime { get; private set; }

		// PRIVATE MEMBERS

		[SerializeField]
		private TextMeshProUGUI _killer;
		[SerializeField]
		private TextMeshProUGUI _victim;
		[SerializeField]
		private Image _deathCauseIcon;
		[SerializeField]
		private GameObject _headshot;
		[SerializeField]
		private DamageIcon[] _damageIcons;
		[SerializeField]
		private Sprite _fallbackDeathIcon;
		[SerializeField]
		private Color _joinedLeftColor;
		[SerializeField]
		private Color _eliminatedColor;
		[SerializeField]
		private Color _enemyColor = Color.red;
		[SerializeField]
		private Color _localPlayerColor = Color.yellow;

		private Dictionary<EHitType, Sprite> _damageIconsMap = new Dictionary<EHitType, Sprite>();

		// PUBLIC METHODS

		public void SetData(IFeedData data)
		{
			if (data is KillFeedData killData)
			{
				_victim.text = killData.Victim;
				_victim.color = killData.VictimIsLocal == true ? _localPlayerColor : _enemyColor;

				_killer.SetActive(killData.Killer.HasValue());
				_killer.text = killData.Killer;
				_killer.color = killData.KillerIsLocal == true ? _localPlayerColor : _enemyColor;

				_headshot.SetActive(killData.IsHeadshot);

				_damageIconsMap.TryGetValue(killData.DamageType, out var causeIcon);

				_deathCauseIcon.sprite = causeIcon != null ? causeIcon : _fallbackDeathIcon;
				_deathCauseIcon.SetActive(true);
			}
			else if (data is JoinedLeftFeedData joinedLeftData)
			{
				_headshot.SetActive(false);
				_killer.SetActive(false);
				_deathCauseIcon.SetActive(false);
				_victim.color = _joinedLeftColor;

				if (joinedLeftData.Joined == true)
				{
					_victim.text  = $"{joinedLeftData.Nickname} joined the game";
				}
				else
				{
					_victim.text = $"{joinedLeftData.Nickname} left the game";
				}
			}
			else if (data is EliminationFeedData eliminationData)
			{
				_headshot.SetActive(false);
				_killer.SetActive(false);
				_deathCauseIcon.SetActive(false);

				_victim.text  = $"{eliminationData.Nickname} eliminated";
				_victim.color = _eliminatedColor;
			}
			else if (data is AnnouncementFeedData announcementData)
			{
				_headshot.SetActive(false);
				_killer.SetActive(false);
				_deathCauseIcon.SetActive(false);
				
				_victim.text  = announcementData.Announcement;
				_victim.color = announcementData.Color;
			}

			VisibilityTime = 0f;
		}

		// MONOBEHAVIOUR

		protected void Awake()
		{
			for (int i = 0; i < _damageIcons.Length; i++)
			{
				_damageIconsMap[_damageIcons[i].Type] = _damageIcons[i].Icon;
			}
		}

		protected void Update()
		{
			VisibilityTime += Time.deltaTime;
		}

		// HELPERS

		[System.Serializable]
		private class DamageIcon
		{
			public EHitType Type;
			public Sprite      Icon;
		}
	}
}
