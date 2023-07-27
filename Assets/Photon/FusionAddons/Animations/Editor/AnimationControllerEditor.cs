namespace Fusion.Animations.Editor
{
	using UnityEngine;
	using UnityEditor;

	[CustomEditor(typeof(AnimationController), true)]
	public class AnimationControllerEditor : Editor
	{
		// Editor INTERFACE

		public override bool RequiresConstantRepaint()
		{
			return true;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (Application.isPlaying == false)
				return;

			AnimationController controller = target as AnimationController;

			Color defaultColor            = GUI.color;
			Color defaultContentColor     = GUI.contentColor;
			Color defaultBackgroundColor  = GUI.backgroundColor;
			Color enabledBackgroundColor  = Color.green;
			Color disabledBackgroundColor = defaultBackgroundColor;

			DrawLine(Color.gray);

			EditorGUILayout.BeginHorizontal();
			{
				EditorGUILayout.BeginVertical();
				{
					GUI.backgroundColor = controller.HasInputAuthority == true ? enabledBackgroundColor : disabledBackgroundColor;
					GUILayout.Button("Input Authority");
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginVertical();
				{
					GUI.backgroundColor = controller.HasStateAuthority == true ? enabledBackgroundColor : disabledBackgroundColor;
					GUILayout.Button("State Authority");
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndHorizontal();

			GUI.backgroundColor = defaultBackgroundColor;

			DrawLine(Color.gray);

			EditorGUILayout.Toggle("Has Manual Update", controller.HasManualUpdate);

			DrawLine(Color.gray);

			// Add package "com.unity.playablegraph-visualizer": "https://github.com/Unity-Technologies/graph-visualizer.git"
			// And uncomment following lines to show Playable Graph Visualizer
			/*if (GUILayout.Button("Show Graph") == true)
			{
				GraphVisualizer.GraphVisualizerClient.ClearGraphs();
				GraphVisualizer.GraphVisualizerClient.Show(controller.Graph);
				GraphVisualizer.PlayableGraphVisualizerWindow.ShowWindow();
			}

			DrawLine(Color.gray);*/

			GUI.color           = defaultColor;
			GUI.contentColor    = defaultContentColor;
			GUI.backgroundColor = defaultBackgroundColor;
		}

		// PRIVATE METHODS

		public static void DrawLine(Color color, float thickness = 1.0f, float padding = 10.0f)
		{
			Rect controlRect = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));

			controlRect.height = thickness;
			controlRect.y += padding * 0.5f;

			EditorGUI.DrawRect(controlRect, color);
		}
	}
}
