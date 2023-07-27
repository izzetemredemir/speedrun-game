//#define ENABLE_LOGS

using Fusion;

namespace TPSBR
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Events;
	using UnityEngine.SceneManagement;

	using UnityScene = UnityEngine.SceneManagement.Scene;

	public sealed class NetworkSceneManager : Fusion.Behaviour, INetworkSceneManager, INetworkSceneManagerObjectResolver
	{
		public Scene GameplayScene => _gameplayScene;

		private NetworkRunner                   _runner;
		private Dictionary<Guid, NetworkObject> _sceneObjects = new Dictionary<Guid, NetworkObject>();
		private SceneRef                        _currentScene;
		private Scene                           _gameplayScene;
		private int                             _instanceID;

		private static int       _loadingInstance;
		private static Coroutine _loadingCoroutine;
		private static Scene     _activationScene;
		private static float     _activationTimeout;

		void INetworkSceneManager.Initialize(NetworkRunner runner)
		{
			_runner = runner;
	        _sceneObjects.Clear();
	        _currentScene = SceneRef.None;
	        _gameplayScene = null;

	        Log($"Initialize");
		}

		void INetworkSceneManager.Shutdown(NetworkRunner runner)
		{
			if (_loadingInstance == _instanceID)
			{
				Log($"Stopping scene load");

				try
				{
					if (_loadingCoroutine != null)
					{
						Log($"Stopping coroutine");
						StopCoroutine(_loadingCoroutine);
					}
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}

				_loadingInstance  = default;
				_loadingCoroutine = default;
			}

	        Log($"Shutdown");

	        _runner = null;
	        _sceneObjects.Clear();
	        _currentScene = SceneRef.None;
	        _gameplayScene = null;
		}

		bool INetworkSceneManager.IsReady(NetworkRunner runner)
		{
			if (_loadingInstance == _instanceID)
				return false;
			if (_gameplayScene == null || _gameplayScene.ContextReady == false)
				return false;
			if (_currentScene != _runner.CurrentScene)
				return false;

			return true;
		}

		bool INetworkSceneManagerObjectResolver.TryResolveSceneObject(NetworkRunner runner, Guid sceneObjectGuid, out NetworkObject instance)
		{
			if (_sceneObjects.TryGetValue(sceneObjectGuid, out instance) == false)
			{
				Debug.LogError($"Failed to resolve scene object with Guid {sceneObjectGuid}");
				return false;
			}

			return true;
		}

		private void Awake()
		{
			_instanceID = GetInstanceID();
		}

		private void LateUpdate()
		{
			if (_runner == null)
				return;
			if (_loadingCoroutine != null)
				return;
			if (_currentScene == _runner.CurrentScene)
				return;
			if (Time.realtimeSinceStartup < _activationTimeout && _activationScene != null && _activationScene.IsActive == false)
				return;

			_activationScene = null;

			Log($"Starting scene load");

			_loadingInstance  = _instanceID;
			_loadingCoroutine = StartCoroutine(SwitchSceneCoroutine(_runner, _currentScene, _runner.CurrentScene));
		}

		private IEnumerator SwitchSceneCoroutine(NetworkRunner runner, SceneRef fromScene, SceneRef toScene)
		{
			_currentScene  = SceneRef.None;
			_gameplayScene = null;

			try
			{
				runner.InvokeSceneLoadStart();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				FinishSceneLoading();
				yield break;
			}

			if (runner.Config.PeerMode == NetworkProjectConfig.PeerModes.Single)
			{
				UnityScene loadedScene = default;
				UnityScene activeScene = SceneManager.GetActiveScene();

				bool canTakeOverActiveScene = fromScene == default && IsScenePathOrNameEqual(activeScene, toScene);

				if (canTakeOverActiveScene == true)
				{
					loadedScene = activeScene;
				}
				else
				{
					if (TryGetScenePathFromBuildSettings(toScene, out string scenePath) == false)
					{
						Debug.LogError($"Unable to find scene {toScene}");
						FinishSceneLoading();
						yield break;
					}

					UnityAction<UnityScene, LoadSceneMode> onSceneLoaded = (scene, loadSceneMode) =>
					{
						if (loadedScene == default && IsScenePathOrNameEqual(scene, scenePath) == true)
						{
							loadedScene = scene;
						}
					};

					SceneManager.sceneLoaded += onSceneLoaded;

					Log($"Loading scene {scenePath}");

					yield return SceneManager.LoadSceneAsync(scenePath, new LoadSceneParameters(LoadSceneMode.Additive));

					float timeout = 2.0f;
					while (timeout > 0.0f && loadedScene.IsValid() == false)
					{
						yield return null;
						timeout -= Time.unscaledDeltaTime;
					}

					SceneManager.sceneLoaded -= onSceneLoaded;

					if (loadedScene.IsValid() == false)
					{
						Debug.LogError($"Unable to load scene {toScene}");
						FinishSceneLoading();
						yield break;
					}

					Log($"Loaded scene {loadedScene.name}");

					SceneManager.SetActiveScene(loadedScene);
				}

				FindNetworkObjects(loadedScene, true, false);
			}
			else
			{
				UnityScene activeScene = SceneManager.GetActiveScene();

				bool canTakeOverActiveScene = fromScene == default && IsScenePathOrNameEqual(activeScene, toScene);

				LoadSceneParameters loadSceneParameters = new LoadSceneParameters(LoadSceneMode.Additive, NetworkProjectConfig.ConvertPhysicsMode(runner.Config.PhysicsEngine));

				UnityScene   sceneToUnload    = runner.MultiplePeerUnityScene;
				GameObject[] transientObjects = runner.IsMultiplePeerSceneTemp == true ? sceneToUnload.GetRootGameObjects() : Array.Empty<GameObject>();

				if (canTakeOverActiveScene == true && NetworkRunner.GetRunnerForScene(activeScene) == null && SceneManager.sceneCount > 1)
				{
					Log($"Unloading scene {activeScene.name}");
					yield return SceneManager.UnloadSceneAsync(activeScene);
				}

				if (SceneManager.sceneCount == 1 && transientObjects.Length == 0)
				{
					loadSceneParameters.loadSceneMode = LoadSceneMode.Single;
				}
				else if (sceneToUnload.IsValid() == true)
				{
					if (runner.TryMultiplePeerAssignTempScene() == true)
					{
						Log($"Unloading scene {sceneToUnload.name}");
						yield return SceneManager.UnloadSceneAsync(sceneToUnload);
					}
				}

				if (TryGetScenePathFromBuildSettings(toScene, out string scenePath) == false)
				{
					Debug.LogError($"Unable to find scene {toScene}");
					FinishSceneLoading();
					yield break;
				}

				UnityScene loadedScene = default;

				UnityAction<UnityScene, LoadSceneMode> onSceneLoaded = (scene, loadSceneMode) =>
				{
					if (loadedScene == default && IsScenePathOrNameEqual(scene, scenePath) == true)
					{
						loadedScene = scene;
					}
				};

				SceneManager.sceneLoaded += onSceneLoaded;

				Log($"Loading scene {scenePath}");

				yield return SceneManager.LoadSceneAsync(scenePath, loadSceneParameters);

				float timeout = 2.0f;
				while (timeout > 0.0f && loadedScene.IsValid() == false)
				{
					yield return null;
					timeout -= Time.unscaledDeltaTime;
				}

				SceneManager.sceneLoaded -= onSceneLoaded;

				if (loadedScene.IsValid() == false)
				{
					Debug.LogError($"Unable to load scene {toScene}");
					FinishSceneLoading();
					yield break;
				}

				Log($"Loaded scene {loadedScene.name}");

				FindNetworkObjects(loadedScene, true, true);

				sceneToUnload = runner.MultiplePeerUnityScene;

				runner.MultiplePeerUnityScene = loadedScene;

				if (sceneToUnload.IsValid() == true)
				{
					if (transientObjects.Length > 0)
					{
						Log($"Moving {transientObjects.Length} transient objects to scene {loadedScene.name}");
						foreach (GameObject transientObject in transientObjects)
						{
							SceneManager.MoveGameObjectToScene(transientObject, loadedScene);
						}
					}

					Log($"Unloading scene {sceneToUnload.name}");
					yield return SceneManager.UnloadSceneAsync(sceneToUnload);
				}
			}

			_currentScene      = runner.CurrentScene;
			_gameplayScene     = runner.SimulationUnityScene.GetComponent<Scene>(true);
			_activationScene   = _gameplayScene;
			_activationTimeout = Time.realtimeSinceStartup + 10.0f;

			float contextTimeout = 20.0f;
			while (_gameplayScene.ContextReady == false && contextTimeout > 0.0f)
			{
				Log($"Waiting for scene context");
				yield return null;
				contextTimeout -= Time.unscaledDeltaTime;
			}

			if (_gameplayScene.ContextReady == false)
			{
				_currentScene  = SceneRef.None;
				_gameplayScene = null;

				Debug.LogError($"Scene context is not ready (timeout)!");
				FinishSceneLoading();
				yield break;
			}

			var contextBehaviours = runner.SimulationUnityScene.GetComponents<IContextBehaviour>(true);
			foreach (var behaviour in contextBehaviours)
			{
				behaviour.Context = _gameplayScene.Context;
			}

			try
			{
				runner.RegisterSceneObjects(_sceneObjects.Values);
				runner.InvokeSceneLoadDone();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				FinishSceneLoading();
				yield break;
			}

			FinishSceneLoading();
		}

		private void FindNetworkObjects(UnityScene scene, bool disable, bool addVisibilityNodes)
		{
			_sceneObjects.Clear();

			List<NetworkObject> networkObjects = new List<NetworkObject>();

			foreach (GameObject rootGameObject in scene.GetRootGameObjects())
			{
				networkObjects.Clear();
				rootGameObject.GetComponentsInChildren(true, networkObjects);

				foreach (NetworkObject networkObject in networkObjects)
				{
					if (networkObject.Flags.IsSceneObject() == true)
					{
						if (networkObject.gameObject.activeInHierarchy == true || networkObject.Flags.IsActivatedByUser() == true)
						{
							_sceneObjects.Add(networkObject.NetworkGuid, networkObject);
							Log($"Found networked scene object {networkObject.name} ({networkObject.NetworkGuid})");
						}
					}
				}

				if (addVisibilityNodes == true)
				{
					RunnerVisibilityNode.AddVisibilityNodes(rootGameObject, _runner);
				}
			}

			if (disable == true)
			{
				foreach (NetworkObject sceneObject in _sceneObjects.Values)
				{
					sceneObject.gameObject.SetActive(false);
				}
			}
		}

		private void FinishSceneLoading()
		{
			Log($"Finishing scene load");

			_loadingInstance  = default;
			_loadingCoroutine = default;
		}

		[System.Diagnostics.Conditional("ENABLE_LOGS")]
		private void Log(string message)
		{
			Debug.Log($"[{Time.frameCount}] NetworkSceneManager({_instanceID}): {message}");
		}

		private static bool IsScenePathOrNameEqual(UnityScene scene, string nameOrPath)
		{
			return scene.path == nameOrPath || scene.name == nameOrPath;
		}

		private static bool IsScenePathOrNameEqual(UnityScene scene, SceneRef sceneRef)
		{
			return TryGetScenePathFromBuildSettings(sceneRef, out var path) == true ? IsScenePathOrNameEqual(scene, path) : false;
		}

		private static bool TryGetScenePathFromBuildSettings(SceneRef sceneRef, out string path)
		{
			if (sceneRef.IsValid == true)
			{
				path = SceneUtility.GetScenePathByBuildIndex(sceneRef);
				if (string.IsNullOrEmpty(path) == false)
					return true;
			}

			path = string.Empty;
			return false;
		}
	}
}
