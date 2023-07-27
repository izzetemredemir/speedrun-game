using System;
using UnityEngine;

namespace TPSBR
{
	[CreateAssetMenu(menuName = "TPSBR/Footstep Setup")]
	public class FootstepSetup : ScriptableObject
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private AudioSetup _fallbackWalkSound;
		[SerializeField]
		private AudioSetup _fallbackRunSound;
		[SerializeField]
		private FootstepData[] _footsteps;

		[NonSerialized]
		private bool _initialized;
		[NonSerialized]
		private int _untaggedHash;

		// PUBLIC METHODS

		public AudioSetup GetSound(int tagHash, bool isRunning)
		{
			if (tagHash == 0 || tagHash == _untaggedHash)
				return isRunning == true ? _fallbackRunSound : _fallbackWalkSound;

			if (_initialized == false)
			{
				_untaggedHash = "Untagged".GetHashCode();

				for (int i = 0; i < _footsteps.Length; i++)
				{
					_footsteps[i].TagHash = _footsteps[i].Tag.GetHashCode();
				}

				_initialized = true;
			}

			for (int i = 0; i < _footsteps.Length; i++)
			{
				if (_footsteps[i].TagHash == tagHash)
					return isRunning == true ? _footsteps[i].SoundRun : _footsteps[i].SoundWalk;
			}

			return isRunning == true ? _fallbackRunSound : _fallbackWalkSound;
		}

		// HELPERS

		[Serializable]
		private class FootstepData
		{
			public string Tag;
			public AudioSetup SoundWalk;
			public AudioSetup SoundRun;

			[NonSerialized]
			public int TagHash;
		}
	}
}
