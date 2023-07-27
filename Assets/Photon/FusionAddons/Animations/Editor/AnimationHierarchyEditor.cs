namespace Fusion.Animations.Editor
{
	using UnityEngine;
	using UnityEditor;

	using AnimationState = Fusion.Animations.AnimationState;

	[InitializeOnLoad]
	public static class AnimationHierarchyEditor
	{
		static AnimationHierarchyEditor()
		{
			EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
		}

		private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
		{
			if (Application.isPlaying == false)
				return;

			Object instance = EditorUtility.InstanceIDToObject(instanceID);
			if (instance is GameObject gameObject)
			{
				AnimationState animationState = gameObject.GetComponentNoAlloc<AnimationState>();
				if (animationState != null && animationState.IsActive() == true)
				{
					selectionRect.min = new Vector2(selectionRect.max.x - selectionRect.height, selectionRect.min.y);
					EditorGUI.DrawRect(selectionRect, Color.green);
					return;
				}

				AnimationLayer animationLayer = gameObject.GetComponentNoAlloc<AnimationLayer>();
				if (animationLayer != null && animationLayer.IsActive() == true)
				{
					selectionRect.min = new Vector2(selectionRect.max.x - selectionRect.height, selectionRect.min.y);
					EditorGUI.DrawRect(selectionRect, Color.green);
					return;
				}
			}
		}
	}
}
