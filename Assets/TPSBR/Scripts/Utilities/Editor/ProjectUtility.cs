namespace TPSBR
{
	using System;
	using UnityEngine;
	using UnityEditor;
	using Fusion.Photon.Realtime;

	public static class ProjectUtility
	{
		// PUBLIC METHODS

		[MenuItem("TPSBR/Prepare Regular Build")]
		public static void PrepareRegularBuild()
		{
			PhotonAppSettings.Instance.AppSettings.AppVersion = $"{Application.version}-{DateTime.Now.ToString("yyMMdd")}";
			EditorUtility.SetDirty(PhotonAppSettings.Instance);

			GlobalSettings globalSettings = Resources.LoadAll<GlobalSettings>("")[0];
			globalSettings.Network.QueueName = $"Queue-Build-{DateTime.Now.ToString("yyMMdd")}";
			EditorUtility.SetDirty(globalSettings.Network);

			AssetDatabase.SaveAssets();
		}

		[MenuItem("TPSBR/Prepare Public Build")]
		public static void PreparePublicBuild()
		{
			PhotonAppSettings.Instance.AppSettings.AppVersion = $"{Application.version}-{DateTime.Now.ToString("yyMMdd")}-public";
			EditorUtility.SetDirty(PhotonAppSettings.Instance);

			GlobalSettings globalSettings = Resources.LoadAll<GlobalSettings>("")[0];
			globalSettings.Network.QueueName = $"Queue-Build-{DateTime.Now.ToString("yyMMdd")}";
			EditorUtility.SetDirty(globalSettings.Network);

			AssetDatabase.SaveAssets();
		}
	}
}
