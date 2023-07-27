namespace TPSBR.Editor
{
	using UnityEditor;

	public static class AllowHttpDialog
	{
#if UNITY_2022_1_OR_NEWER
		[UnityEditor.Callbacks.DidReloadScripts]
		private static void OnScriptsReloaded()
		{
			if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneLinux64)
				return;
			if (PlayerSettings.insecureHttpOption == InsecureHttpOption.AlwaysAllowed)
				return;

			const string key = "TPSBR.AllowHttpChecked";

			if (EditorPrefs.HasKey(key) == false)
			{
				EditorPrefs.SetBool(key, true);

				bool result = EditorUtility.DisplayDialog("Action required!", "Running Linux server build on Multiplay platform requires HTTP requests to be enabled in Player Settings.", "The build will be hosted on Multiplay, enable HTTP requests.", "Ignore");
				if (result == true)
				{
					PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
					AssetDatabase.SaveAssets();
				}
			}
		}
#endif
	}
}
