using UnityEditor;
using UnityEngine;

namespace Unity.CV.SyntheticHumans.Editor
{
    // We need this because AssetPoolFilters are meant to be drawn as nested properties in the AssetPool filter list
    // editor. This class simply draws all of the nested properties in a given subclass of AssetPoolFilter
    [CustomPropertyDrawer(typeof(AssetPoolFilter))]
    class AssetPoolFilterPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var it = new SerializedObject(property.objectReferenceValue).GetIterator();

            it.NextVisible(true);
            return EditorGUIUtility.singleLineHeight * (it.CountRemaining());
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var serializedObj = new SerializedObject(property.objectReferenceValue);
            var it = serializedObj.GetIterator();
            position.height = EditorGUIUtility.singleLineHeight;

            it.NextVisible(true);
            while (it.NextVisible(false)) {
                EditorGUI.PropertyField(position, it);
                position.y += EditorGUIUtility.singleLineHeight;
            }

            serializedObj.ApplyModifiedProperties();
        }
    }
}
