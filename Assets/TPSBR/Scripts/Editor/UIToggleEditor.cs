using UnityEditor;
using UnityEditor.UI;

namespace TPSBR.UI
{
	[CustomEditor(typeof(UIToggle), true)]
	public class UIToggleEditor : ToggleEditor
	{
		// PRIVATE METHODS

		private SerializedProperty _playValueChangedSound;
		private SerializedProperty _customValueChangedSound;

		// ButtonEditor INTERFACE

		protected override void OnEnable()
		{
			base.OnEnable();

			_playValueChangedSound = serializedObject.FindProperty("_playValueChangedSound");
			_customValueChangedSound = serializedObject.FindProperty("_customValueChangedSound");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.PropertyField(_playValueChangedSound);

			if (_playValueChangedSound.boolValue == true)
			{
				EditorGUILayout.PropertyField(_customValueChangedSound);
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
