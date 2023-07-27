namespace Fusion.Animations.Editor
{
	using UnityEngine;
	using UnityEditor;

	using AnimationState = Fusion.Animations.AnimationState;

	[CustomEditor(typeof(AnimationState), true)]
	public class AnimationStateEditor : Editor
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

			AnimationState state = target as AnimationState;

			Color defaultColor            = GUI.color;
			Color defaultContentColor     = GUI.contentColor;
			Color defaultBackgroundColor  = GUI.backgroundColor;
			Color enabledBackgroundColor  = Color.green;
			Color disabledBackgroundColor = defaultBackgroundColor;

			DrawLine(Color.gray);

			EditorGUILayout.Toggle("Is Active", state.IsActive());
			EditorGUILayout.LabelField("Weight", state.Weight.ToString("0.00"));
			EditorGUILayout.LabelField("Fading Speed", state.FadingSpeed.ToString("0.00"));

			DrawLine(Color.gray);

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
