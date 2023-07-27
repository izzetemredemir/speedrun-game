using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
	public sealed class ObjectCache : SceneService
	{
		// PUBLIC MEMBERS

		public int CachedCount   { get { return _all.Count;      } }
		public int BorrowedCount { get { return _borrowed.Count; } }

		// PRIVATE MEMBERS

		[SerializeField]
		private bool _hideCachedObjectsInHierarchy = true;
		[SerializeField]
		private List<CacheObject> _precacheObjects;

		private readonly Dictionary<GameObject, Stack<GameObject>> _cached   = new Dictionary<GameObject, Stack<GameObject>>();
		private readonly Dictionary<GameObject, GameObject>        _borrowed = new Dictionary<GameObject, GameObject>();
		private readonly List<DeferredReturn>                      _deferred = new List<DeferredReturn>();
		private readonly Stack<DeferredReturn>                     _pool     = new Stack<DeferredReturn>();
		private readonly List<GameObject>                          _all      = new List<GameObject>();

		// PUBLIC METHODS

		public T Get<T>(T prefab, bool activate = true, bool createIfEmpty = true) where T : UnityEngine.Component
		{
			return Get(prefab, null, activate, createIfEmpty);
		}

		public GameObject Get(GameObject prefab, bool activate = true, bool createIfEmpty = true)
		{
			return Get(prefab, null, activate, createIfEmpty);
		}

		public T Get<T>(T prefab, Transform parent, bool activate = true, bool createIfEmpty = true) where T : UnityEngine.Component
		{
			GameObject instance = Get(prefab.gameObject, parent, activate, createIfEmpty);
			return instance != null ? instance.GetComponent<T>() : null;
		}

		public GameObject Get(GameObject prefab, Transform parent, bool activate = true, bool createIfEmpty = true)
		{
			if (_cached.TryGetValue(prefab, out Stack<GameObject> stack) == false)
			{
				stack = new Stack<GameObject>();
				_cached[prefab] = stack;
			}

			if (stack.Count == 0)
			{
				if (createIfEmpty == true)
				{
					CreateInstance(prefab);
				}
				else
				{
					Debug.LogWarningFormat("Prefab {0} not available in cache, returning NULL", prefab.name);
					return null;
				}
			}

			GameObject instance = stack.Pop();

			_borrowed[instance] = prefab;

			Transform instanceTransform = instance.transform;

			if (parent != null)
			{
				instanceTransform.SetParent(parent, false);
			}

			instanceTransform.localPosition = Vector3.zero;
			instanceTransform.localRotation = Quaternion.identity;
			instanceTransform.localScale    = Vector3.one;

			if (activate == true)
			{
				instance.SetActive(true);
			}

#if UNITY_EDITOR
			if (_hideCachedObjectsInHierarchy == true)
			{
				instance.hideFlags &= ~HideFlags.HideInHierarchy;
			}
#endif
			return instance;
		}

		public void Return(UnityEngine.Component component, bool deactivate = true)
		{
			Return(component.gameObject, deactivate);
		}

		public void Return(GameObject instance, bool deactivate = true)
		{
			if (deactivate == true)
			{
				instance.SetActive(false);
			}

			instance.transform.SetParent(null, false);

			_cached[_borrowed[instance]].Push(instance);
			_borrowed.Remove(instance);

#if UNITY_EDITOR
			if (_hideCachedObjectsInHierarchy == true)
			{
				instance.hideFlags |= HideFlags.HideInHierarchy;
			}
#endif
		}

		public void ReturnRange(List<GameObject> instances, bool deactivate = true)
		{
			for (int i = 0; i < instances.Count; i++)
			{
				Return(instances[i], deactivate);
			}
		}

		public void ReturnDeferred(GameObject instance, float delay)
		{
			DeferredReturn toReturn = _pool.Count > 0 ? _pool.Pop() : new DeferredReturn();
			toReturn.GameObject = instance;
			toReturn.Delay      = delay;

			_deferred.Add(toReturn);
		}

		public void Prepare(GameObject prefab, int desiredCount)
		{
			if (_cached.TryGetValue(prefab, out Stack<GameObject> stack) == false)
			{
				stack = new Stack<GameObject>();
				_cached[prefab] = stack;
			}

			while (stack.Count < desiredCount)
			{
				CreateInstance(prefab);
			}
		}

		// SceneService INTERFACE

		protected override void OnInitialize()
		{
			foreach (CacheObject cacheObject in _precacheObjects)
			{
				_cached[cacheObject.GameObject] = new Stack<GameObject>();

				for (int i = 0; i < cacheObject.Count; ++i)
				{
					CreateInstance(cacheObject.GameObject);
				}
			}
		}

		protected override void OnDeinitialize()
		{
			foreach (var item in _borrowed)
			{
				GameObject go = item.Key;
				bool shouldReturn = go != null;

				foreach (var deferredItem in _deferred)
				{
					if (go == deferredItem.GameObject)
					{
						shouldReturn = false;
						break;
					}
				}

				if (shouldReturn == true)
				{
					Debug.LogWarning($"Object {go.name} from cache was not returned and will be destroyed");
				}
			}

			_deferred.Clear();
			_borrowed.Clear();
			_cached.Clear();

			foreach (GameObject instance in _all)
			{
				Destroy(instance);
			}

			_all.Clear();
		}

		protected override void OnTick()
		{
			for (int i = _deferred.Count; i --> 0;)
			{
				DeferredReturn deferred = _deferred[i];

				deferred.Delay -= Time.deltaTime;
				if (deferred.Delay > 0.0f)
					continue;

				_deferred.RemoveBySwap(i);
				Return(deferred.GameObject, true);

				deferred.Reset();
				_pool.Push(deferred);
			}
		}

		// PRIVATE METHODS

		private void CreateInstance(GameObject prefab)
		{
			GameObject instance = Instantiate(prefab, null, false);
			instance.name = prefab.name;

			instance.SetActive(false);
			_cached[prefab].Push(instance);
			_all.Add(instance);

#if UNITY_EDITOR
			if (_hideCachedObjectsInHierarchy == true)
			{
				instance.hideFlags |= HideFlags.HideInHierarchy;
			}
#endif
		}

		// HELPERS

		[Serializable]
		private sealed class CacheObject
		{
			public int        Count;
			public GameObject GameObject;
		}

		private sealed class DeferredReturn
		{
			public GameObject GameObject;
			public float      Delay;

			public void Reset()
			{
				GameObject = null;
				Delay      = 0.0f;
			}
		}
	}
}
