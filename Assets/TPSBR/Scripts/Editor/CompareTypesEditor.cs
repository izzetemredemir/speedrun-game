namespace TPSBR.Editor
{
	using System.Collections.Generic;
	using UnityEditor;
	using UnityEngine;

	public sealed class CompareTypesEditor : EditorWindow
    {
		private static TextAsset    _file01;
		private static TextAsset    _file02;
		private static Vector2      _scroll01;
		private static Vector2      _scroll02;
		private static List<string> _fileDiff01 = new List<string> { "---" };
		private static List<string> _fileDiff02 = new List<string> { "---" };

		[MenuItem("Fusion/Windows/Compare Types Inspector")]
		public static void ShowWindow()
		{
			var window = GetWindow(typeof(CompareTypesEditor), false, "Compare Types");
			window.minSize = new Vector2(400, 300);
		}

		private void OnGUI()
		{
			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();
			TextAsset file01 = EditorGUILayout.ObjectField(_file01, typeof(TextAsset), false) as TextAsset;
			TextAsset file02 = EditorGUILayout.ObjectField(_file02, typeof(TextAsset), false) as TextAsset;
			GUILayout.EndHorizontal();

			if (file01 != _file01 || file02 != _file02)
			{
				_file01 = file01;
				_file02 = file02;

				_fileDiff01.Clear();
				_fileDiff02.Clear();

				if (file01 == null || file02 == null)
				{
					_fileDiff01.Add("---");
					_fileDiff02.Add("---");
				}
				else
				{
					string[] types01 = _file01.text.Split('\n');
					string[] types02 = _file02.text.Split('\n');

					for (int i = 0; i < types01.Length; ++i)
					{
						string type01 = types01[i];
						if (types02.Contains(type01) == false)
						{
							_fileDiff01.Add(type01);
						}
					}

					for (int i = 0; i < types02.Length; ++i)
					{
						string type02 = types02[i];
						if (types01.Contains(type02) == false)
						{
							_fileDiff02.Add(type02);
						}
					}
				}
			}

			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical();
			_scroll01 = GUILayout.BeginScrollView(_scroll01);
			for (int i = 0; i < _fileDiff01.Count; ++i)
			{
				GUILayout.Label(_fileDiff01[i]);
			}
			GUILayout.EndScrollView();
			GUILayout.EndVertical();
			GUILayout.BeginVertical();
			_scroll02 = GUILayout.BeginScrollView(_scroll02);
			for (int i = 0; i < _fileDiff02.Count; ++i)
			{
				GUILayout.Label(_fileDiff02[i]);
			}
			GUILayout.EndScrollView();
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();

			GUILayout.EndVertical();
		}
	}
}
