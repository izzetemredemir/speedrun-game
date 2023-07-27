using UnityEngine;

namespace TPSBR
{
	[CreateAssetMenu(menuName = "TPSBR/Recoil Pattern")]
	public class RecoilPattern : ScriptableObject
	{
		// PUBLIC MEMBERS

		public Vector2[] RecoilStartValues   => _recoilStartValues;
		public Vector2[] RecoilEndlessValues => _recoilEndlessValues;

		// PRIVATE MEMBERS

		[SerializeField]
		private Vector2[] _recoilStartValues;
		[SerializeField]
		private Vector2[] _recoilEndlessValues;

		// PUBLIC METHODS

		public Vector2 GetRecoil(int index)
		{
			if (index < 0)
				return Vector2.zero;

			int startCount = _recoilStartValues.Length;

			if (index < startCount)
				return _recoilStartValues[index];

			int endlessCount = _recoilEndlessValues.Length;

			if (endlessCount == 0)
				return Vector2.zero;

			index = (index - startCount) % endlessCount;

			return _recoilEndlessValues[index];
		}
	}
}
