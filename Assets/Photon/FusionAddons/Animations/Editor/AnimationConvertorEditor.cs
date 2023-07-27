namespace Fusion.Animations.Editor
{
	using UnityEngine;
	using UnityEditor;

	[CustomEditor(typeof(AnimationConvertor), true)]
	public class AnimationConvertorEditor : Editor
	{
		// Editor INTERFACE

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (Application.isPlaying == true)
				return;

			AnimationConvertor convertor = target as AnimationConvertor;

			DrawLine(Color.gray);

			if (GUILayout.Button("Convert Clips") == true)
			{
				convertor.ConvertClips();
			}
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
