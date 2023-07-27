using UnityEngine;

namespace TPSBR
{
	public abstract class CoreBehaviour : MonoBehaviour
	{
		// PUBLIC MEMBERS

		public new string name
		{
			get
			{
#if UNITY_EDITOR
				if (Application.isPlaying == false)
					return base.name;
#endif
				if (_nameCached == false)
				{
					_cachedName = base.name;
					_nameCached = true;
				}

				return _cachedName;
			}
			set
			{
				if (string.CompareOrdinal(_cachedName, value) != 0)
				{
					base.name = value;
					_cachedName = value;
					_nameCached = true;
				}
			}
		}

		public new GameObject gameObject
		{
			get
			{
#if UNITY_EDITOR
				if (Application.isPlaying == false)
					return base.gameObject;
#endif
				if (_gameObjectCached == false)
				{
					_cachedGameObject = base.gameObject;
					_gameObjectCached = true;
				}

				return _cachedGameObject;
			}
		}

		public new Transform transform
		{
			get
			{
#if UNITY_EDITOR
				if (Application.isPlaying == false)
					return base.transform;
#endif
				if (_transformCached == false)
				{
					_cachedTransform = base.transform;
					_transformCached = true;
				}

				return _cachedTransform;
			}
		}

		// PRIVATE MEMBERS

		private string _cachedName;
		private bool _nameCached;
		private GameObject _cachedGameObject;
		private bool _gameObjectCached;
		private Transform _cachedTransform;
		private bool _transformCached;
	}
}
