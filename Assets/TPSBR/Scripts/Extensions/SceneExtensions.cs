namespace TPSBR
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.SceneManagement;

	using UnityScene = UnityEngine.SceneManagement.Scene;

	public static class SceneExtensions
	{
		// PUBLIC METHODS

		public static T GetComponent<T>(this UnityScene scene, bool includeInactive = false) where T : class
		{
			List<GameObject> roots = ListPool<GameObject>.Shared.Get(16);
			scene.GetRootGameObjects(roots);

			T component = default;

			for (int i = 0, count = roots.Count; i < count; ++i)
			{
				component = roots[i].GetComponentInChildren<T>(includeInactive);
				if (component != null)
					break;
			}

			ListPool<GameObject>.Shared.Return(roots);
			return component;
		}

		public static List<T> GetComponents<T>(this UnityScene scene, bool includeInactive = false) where T : class
		{
			List<T>          allComponents    = new List<T>();
			List<T>          objectComponents = ListPool<T>.Shared.Get(16);
			List<GameObject> sceneRootObjects = ListPool<GameObject>.Shared.Get(16);

			scene.GetRootGameObjects(sceneRootObjects);

			for (int i = 0, count = sceneRootObjects.Count; i < count; ++i)
			{
				sceneRootObjects[i].GetComponentsInChildren(includeInactive, objectComponents);
				allComponents.AddRange(objectComponents);
				objectComponents.Clear();
			}

			ListPool<GameObject>.Shared.Return(sceneRootObjects);
			ListPool<T>.Shared.Return(objectComponents);

			return allComponents;
		}

		public static void GetComponents<T>(this UnityScene scene, List<T> components, bool includeInactive = false) where T : class
		{
			List<T>          objectComponents = ListPool<T>.Shared.Get(16);
			List<GameObject> sceneRootObjects = ListPool<GameObject>.Shared.Get(16);

			scene.GetRootGameObjects(sceneRootObjects);
			components.Clear();

			for (int i = 0, count = sceneRootObjects.Count; i < count; ++i)
			{
				sceneRootObjects[i].GetComponentsInChildren(includeInactive, objectComponents);
				components.AddRange(objectComponents);
				objectComponents.Clear();
			}

			ListPool<GameObject>.Shared.Return(sceneRootObjects);
			ListPool<T>.Shared.Return(objectComponents);
		}

		public static void MoveRootGameObjects(this UnityScene scene, UnityScene targetScene)
		{
			List<GameObject> roots = ListPool<GameObject>.Shared.Get(16);
			scene.GetRootGameObjects(roots);

			for (int i = 0, count = roots.Count; i < count; ++i)
			{
				SceneManager.MoveGameObjectToScene(roots[i], targetScene);
			}

			ListPool<GameObject>.Shared.Return(roots);
		}

		public static GameObject CreateGameObject(this UnityScene scene, string name)
		{
			GameObject gameObject = new GameObject(name);
			if (gameObject.scene != scene)
			{
				SceneManager.MoveGameObjectToScene(gameObject, scene);
			}

			return gameObject;
		}

		public static GameObject Instantiate(this UnityScene scene, GameObject original)
		{
			GameObject gameObject = GameObject.Instantiate(original);
			if (gameObject.scene != scene)
			{
				SceneManager.MoveGameObjectToScene(gameObject, scene);
			}

			return gameObject;
		}

		public static T Instantiate<T>(this UnityScene scene, T original) where T : Component
		{
			T          component  = GameObject.Instantiate(original);
			GameObject gameObject = component.gameObject;

			if (gameObject.scene != scene)
			{
				SceneManager.MoveGameObjectToScene(gameObject, scene);
			}

			return component;
		}

		public static Camera FindMainCamera(this UnityScene scene)
		{
			foreach (Camera camera in scene.GetComponents<Camera>(true))
			{
				if (camera.CompareTag("MainCamera") == true)
					return camera;
			}

			return null;
		}
	}
}
