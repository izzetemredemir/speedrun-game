using System;
using UnityEngine;

namespace TPSBR
{
	[CreateAssetMenu(menuName = "TPSBR/Piercing Setup")]
	public class PiercingSetup : ScriptableObject
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private float _fallbackDamageMultiplier = 0f;
		[SerializeField]
		private PiercingData[] _piercing;

		[NonSerialized]
		private bool _initialized;
		[NonSerialized]
		private int _untaggedHash;

		// PUBLIC METHODS

		public float GetDamageMultiplier(int tagHash)
		{
			if (tagHash == 0 || tagHash == _untaggedHash)
				return _fallbackDamageMultiplier;

			if (_initialized == false)
			{
				_untaggedHash = "Untagged".GetHashCode();

				for (int i = 0; i < _piercing.Length; i++)
				{
					_piercing[i].TagHash = _piercing[i].Tag.GetHashCode();
				}

				_initialized = true;
			}

			for (int i = 0; i < _piercing.Length; i++)
			{
				if (_piercing[i].TagHash == tagHash)
					return _piercing[i].DamageMultiplier;
			}

			return _fallbackDamageMultiplier;
		}

		// HELPERS

		[Serializable]
		private class PiercingData
		{
			public string Tag;
			public float DamageMultiplier;

			[NonSerialized]
			public int TagHash;
		}
	}
}
