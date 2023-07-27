using System;
using UnityEngine;

namespace TPSBR
{
	[CreateAssetMenu(menuName = "TPSBR/Impact Setup")]
	public class ImpactSetup : ScriptableObject
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private GameObject _fallbackImpact;
		[SerializeField]
		private ImpactData[] _impacts;

		[NonSerialized]
		private bool _initialized;
		[NonSerialized]
		private int _untaggedHash;

		// PUBLIC METHODS

		public GameObject GetImpact(int tagHash)
		{
			if (tagHash == 0 || tagHash == _untaggedHash)
				return _fallbackImpact;

			if (_initialized == false)
			{
				_untaggedHash = "Untagged".GetHashCode();

				for (int i = 0; i < _impacts.Length; i++)
				{
					_impacts[i].TagHash = _impacts[i].Tag.GetHashCode();
				}

				_initialized = true;
			}

			for (int i = 0; i < _impacts.Length; i++)
			{
				if (_impacts[i].TagHash == tagHash)
					return _impacts[i].Impact;
			}

			return _fallbackImpact;
		}

		// HELPERS

		[Serializable]
		private class ImpactData
		{
			public string Tag;
			public GameObject Impact;

			[NonSerialized]
			public int TagHash;
		}
	}
}