using UnityEngine;
using System;
using Fusion;

namespace TPSBR
{
	[Serializable]
	[CreateAssetMenu(fileName = "AgentSettings", menuName = "TPSBR/Agent Settings")]
	public class AgentSettings : ScriptableObject
	{
		// PUBLIC MEMBERS

		public AgentSetup[] Agents => _agents;

		// PRIVATE MEMBERS

		[SerializeField]
		private AgentSetup[] _agents;

		// PUBLIC METHODS

		public AgentSetup GetAgentSetup(string agentID)
		{
			if (agentID.HasValue() == false)
				return null;

			return _agents.Find(t => t.ID == agentID);
		}

		public AgentSetup GetAgentSetup(NetworkPrefabId prefabId)
		{
			if (prefabId.IsValid == false)
				return null;

			return _agents.Find(t => t.AgentPrefabId == prefabId);
		}

		public AgentSetup GetRandomAgentSetup()
		{
			return _agents[UnityEngine.Random.Range(0, _agents.Length)];
		}
	}

	[Serializable]
	public class AgentSetup
	{
		// PUBLIC MEMBERS

		public string               ID                => _id;
		public string               DisplayName       => _displayName;
		public string               Description       => _description;
		public Sprite               Icon              => _icon;
		public GameObject           AgentPrefab       => _agentPrefab;
		public GameObject           MenuAgentPrefab   => _menuAgentPrefab;

		public NetworkPrefabId      AgentPrefabId
		{
			get
			{
				if (_agentPrefabId.IsValid == false && NetworkProjectConfig.Global.PrefabTable.TryGetId(_agentPrefab.GetComponent<NetworkObject>().NetworkGuid, out var id) == true)
				{
					_agentPrefabId = id;
				}

				return _agentPrefabId;
			}
		}

		// PRIVATE MEMBERS

		[SerializeField]
		private string _id;
		[SerializeField]
		private string _displayName;
		[SerializeField, TextArea(3, 6)]
		private string _description;
		[SerializeField]
		private Sprite _icon;
		[SerializeField]
		private GameObject _agentPrefab;
		[SerializeField]
		private GameObject _menuAgentPrefab;

		[NonSerialized]
		private NetworkPrefabId _agentPrefabId;
	}
}
