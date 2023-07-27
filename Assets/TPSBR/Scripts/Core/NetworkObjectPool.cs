using System.Collections.Generic;
using UnityEngine;
using Fusion;

namespace TPSBR
{

	public class NetworkObjectPool : INetworkObjectPool
	{
		public SceneContext Context { get; set; }

		private Dictionary<NetworkPrefabId, Stack<NetworkObject>> _cached   = new Dictionary<NetworkPrefabId, Stack<NetworkObject>>(32);
		private Dictionary<NetworkObject, NetworkPrefabId>        _borrowed = new Dictionary<NetworkObject, NetworkPrefabId>();

		NetworkObject INetworkObjectPool.AcquireInstance(NetworkRunner runner, NetworkPrefabInfo info)
		{
			if (_cached.TryGetValue(info.Prefab, out var objects) == false)
			{
				objects = _cached[info.Prefab] = new Stack<NetworkObject>();
			}

			if (objects.Count > 0)
			{
				var oldInstance = objects.Pop();
				_borrowed[oldInstance] = info.Prefab;

				oldInstance.SetActive(true);

				return oldInstance;
			}

			if (runner.Config.PrefabTable.TryGetPrefab(info.Prefab, out var original) == false)
				return null;

			var instance = Object.Instantiate(original);
			_borrowed[instance] = info.Prefab;

			AssignContext(instance);

			for (int i = 0; i < instance.NestedObjects.Length; i++)
			{
				AssignContext(instance.NestedObjects[i]);
			}

			return instance;
		}

		void INetworkObjectPool.ReleaseInstance(NetworkRunner runner, NetworkObject instance, bool isSceneObject)
		{
			if (isSceneObject == false && runner.IsShutdown == false)
			{
				if (_borrowed.TryGetValue(instance, out var prefabID) == true)
				{
					_borrowed.Remove(instance);
					_cached[prefabID].Push(instance);

					instance.SetActive(false);
					instance.transform.parent = null;
					instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
				}
				else
				{
					Object.Destroy(instance.gameObject);
				}
			}
			else
			{
				Object.Destroy(instance.gameObject);
			}
		}

		private void AssignContext(NetworkObject instance)
		{
			for (int i = 0, count = instance.NetworkedBehaviours.Length; i < count; i++)
			{
				if (instance.NetworkedBehaviours[i] is IContextBehaviour cachedBehaviour)
				{
					cachedBehaviour.Context = Context;
				}
			}

			for (int i = 0, count = instance.SimulationBehaviours.Length; i < count; i++)
			{
				if (instance.SimulationBehaviours[i] is IContextBehaviour cachedBehaviour)
				{
					cachedBehaviour.Context = Context;
				}
			}
		}
	}
}
