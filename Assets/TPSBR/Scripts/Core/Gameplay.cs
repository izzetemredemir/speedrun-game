namespace TPSBR
{
	using System.Collections;
	using UnityEngine;
	using UnityEngine.SceneManagement;

	using UnityScene = UnityEngine.SceneManagement.Scene;

    public class Gameplay : Scene
    {
		private const string UI_SCENE_NAME = "GameplayUI";

		// PRIVATE MEMBERS

		private UnityScene _UIScene;

		// Scene INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			var contextBehaviours = Context.Runner.SimulationUnityScene.GetComponents<IContextBehaviour>(true);

			foreach (var behaviour in contextBehaviours)
			{
				behaviour.Context = Context;
			}
		}

		protected override IEnumerator OnActivate()
		{
			yield return base.OnActivate();

			var asyncOp = SceneManager.LoadSceneAsync(UI_SCENE_NAME, LoadSceneMode.Additive);
			while (asyncOp.isDone == false)
				yield return null;

			for (int i = SceneManager.sceneCount; i --> 0;)
			{
				var unityScene = SceneManager.GetSceneAt(i);
				if (unityScene.name == UI_SCENE_NAME)
				{
					_UIScene      = unityScene;
					var uiService = _UIScene.GetComponent<UI.SceneUI>(true);

					foreach (GameObject rootObject in unityScene.GetRootGameObjects())
					{
						Context.Runner.MoveToRunnerSceneExtended(rootObject);
					}

					Context.UI = uiService;

					SceneManager.UnloadSceneAsync(unityScene);

					AddService(uiService);

					uiService.Activate();
					break;
				}
			}
		}

		protected override void OnTick()
		{
			if (Context.Runner != null)
			{
				Context.Runner.IsVisible = Context.IsVisible;
			}

			base.OnTick();
		}

		protected override void CollectServices()
		{
			base.CollectServices();

			Context.Map = GetService<SceneMap>();
		}
	}
}
