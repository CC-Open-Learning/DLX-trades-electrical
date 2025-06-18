using UnityEngine;
using UnityEditor;

namespace VARLab.TradesElectrical
{
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ReadOnlyEditorField))]
    public class ReadOnlyEditorFieldDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    EditorGUI.LabelField(position, label, new GUIContent(property.boolValue.ToString()));
                    break;
                case SerializedPropertyType.Float:
                    EditorGUI.LabelField(position, label, new GUIContent(property.floatValue.ToString()));
                    break;
                case SerializedPropertyType.String:
                    EditorGUI.LabelField(position, label, new GUIContent(property.stringValue));
                    break;
                case SerializedPropertyType.ObjectReference:
                    EditorGUI.LabelField(position, label, new GUIContent(property.objectReferenceValue.name));
                    break;
                case SerializedPropertyType.Enum:
                    EditorGUI.LabelField(position, label, new GUIContent(property.enumDisplayNames[property.enumValueIndex]));
                    break;
            }
        }
    }
#endif
}
