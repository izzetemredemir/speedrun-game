using UnityEditor;
using UnityEditor.UI;

namespace TPSBR.UI
{
	[CustomEditor(typeof(UISlider), true)]
	public class UISliderEditor : SliderEditor
	{
		// PRIVATE METHODS

		private SerializedProperty _valueText;
		private SerializedProperty _valueFormat;
		private SerializedProperty _playValueChangedSound;
		private SerializedProperty _customValueChangedSound;

		// ButtonEditor INTERFACE

		protected override void OnEnable()
		{
			base.OnEnable();

			_valueText = serializedObject.FindProperty("_valueText");
			_valueFormat = serializedObject.FindProperty("_valueFormat");
			_playValueChangedSound = serializedObject.FindProperty("_playValueChangedSound");
			_customValueChangedSound = serializedObject.FindProperty("_customValueChangedSound");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.PropertyField(_valueText);
			EditorGUILayout.PropertyField(_valueFormat);

			EditorGUILayout.PropertyField(_playValueChangedSound);

			if (_playValueChangedSound.boolValue == true)
			{
				EditorGUILayout.PropertyField(_customValueChangedSound);
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
