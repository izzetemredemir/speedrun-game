using UnityEngine;
using System;

namespace TPSBR
{
	[Serializable]
	[CreateAssetMenu(fileName = "MapSettings", menuName = "TPSBR/Map Settings")]

	public sealed class MapSettings : ScriptableObject
	{
		// PUBLIC MEMBERS

		public MapSetup[] Maps => _maps;

		// PRIVATE MEMBERS

		[SerializeField]
		private MapSetup[] _maps;

		// PUBLIC METHODS

		public MapSetup GetMapSetup(string mapID)
		{
			if (mapID.HasValue() == false)
				return null;

			return _maps.Find(t => t.ID == mapID);
		}

		public int GetMapIndexFromScenePath(string path)
		{
			return Array.FindIndex(_maps, t => t.ScenePath == path);
		}

		public MapSetup GetRandomMapSetup()
		{
			return _maps[UnityEngine.Random.Range(0, _maps.Length)];
		}
	}

	// ===========================================================================

	[Serializable]
	public sealed class MapSetup
	{
		// PUBLIC MEMBERS

		public string ID                  => _id;
		public string ScenePath           => _scenePath;
		public string DisplayName         => _displayName;
		public string Description         => _description;
		public Sprite Image               => _image;
		public int    RecommendedPlayers  => _recommendedPlayers;
		public int    MaxPlayers          => _maxPlayers;
		public bool   ShowInMapSelection  => _showInMapSelection;

		// PRIVATE MEMBERS

		[SerializeField]
		private string _id;
		[SerializeField]
		private string _scenePath;
		[SerializeField]
		private string _displayName;
		[SerializeField, TextArea(3, 6)]
		private string _description;
		[SerializeField]
		private Sprite _image;
		[SerializeField]
		private int _recommendedPlayers = 100;
		[SerializeField]
		private int _maxPlayers = 200;
		[SerializeField]
		private bool _showInMapSelection = true;
	}
}
