namespace TPSBR
{
	using UnityEngine;
	using UnityEngine.SceneManagement;
	using Fusion;

	public static class NetworkRunnerExtensions
	{
		public static void MoveToRunnerSceneExtended(this NetworkRunner runner, GameObject gameObject)
		{
			if (gameObject.scene == runner.SimulationUnityScene)
				return;

			if (runner.Config.PeerMode != NetworkProjectConfig.PeerModes.Single)
			{
				RunnerVisibilityNode.AddVisibilityNodes(gameObject, runner);
			}

			SceneManager.MoveGameObjectToScene(gameObject, runner.SimulationUnityScene);
		}

		public static void MoveToRunnerSceneExtended(this NetworkRunner runner, Component component)
		{
			runner.MoveToRunnerSceneExtended(component.gameObject);
		}
	}
}
