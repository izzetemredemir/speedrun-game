using UnityEditor;
using UnityEditor.UI;

namespace TPSBR.UI
{
	[CustomEditor(typeof(UIButton), true)]
	public class UIButtonEditor : ButtonEditor
	{
		// PRIVATE METHODS

		private SerializedProperty _playClickSound;
		private SerializedProperty _customClickSound;

		// ButtonEditor INTERFACE

		protected override void OnEnable()
		{
			base.OnEnable();

			_playClickSound = serializedObject.FindProperty("_playClickSound");
			_customClickSound = serializedObject.FindProperty("_customClickSound");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.PropertyField(_playClickSound);

			if (_playClickSound.boolValue == true)
			{
				EditorGUILayout.PropertyField(_customClickSound);
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
