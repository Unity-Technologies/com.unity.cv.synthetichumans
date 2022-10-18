using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.CV.SyntheticHumans.Editor
{
    [CustomEditor(typeof(SyntheticHumanAssetPool))]
    class AssetPoolEditor : UnityEditor.Editor
    {
        SerializedProperty m_FiltersProp;
        ReorderableList m_FiltersList;
        GenericMenu m_FiltersAddMenu;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            // The only custom editor logic we need is for the list of filters, so draw the default inspector items first
            DrawDefaultInspector();

            m_FiltersList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void OnEnable()
        {
            m_FiltersProp = serializedObject.FindProperty("assetPoolFilters");

            m_FiltersList = new ReorderableList(
                serializedObject,
                m_FiltersProp,
                draggable: true,
                displayHeader: true,
                displayAddButton: true,
                displayRemoveButton: true);
            m_FiltersList.drawHeaderCallback = (Rect r) =>
            {
                EditorGUI.LabelField(r, "Filters");
            };
            m_FiltersList.onAddDropdownCallback = ShowAddFilterDropdown;
            m_FiltersList.onRemoveCallback = OnRemoveFilter;
            m_FiltersList.drawElementCallback = OnDrawFilter;
            m_FiltersList.elementHeightCallback = OnFilterElementHeight;

            RefreshFilterClasses();
            AssemblyReloadEvents.afterAssemblyReload += RefreshFilterClasses;
        }

        void OnDisable()
        {
            AssemblyReloadEvents.afterAssemblyReload -= RefreshFilterClasses;
        }

        void RefreshFilterClasses()
        {
            var filterClasses = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
                .Where(t => !t.IsAbstract && typeof(AssetPoolFilter).IsAssignableFrom(t)).ToList();
            m_FiltersAddMenu = new GenericMenu();

            foreach(var filterClass in filterClasses)
            {
                m_FiltersAddMenu.AddItem(
                    new GUIContent(filterClass.Name),
                    false,
                    OnAddFilterClick,
                    filterClass);
            }
        }

        void OnRemoveFilter(ReorderableList l)
        {
            var filter = m_FiltersProp.GetArrayElementAtIndex(l.index).objectReferenceValue;
            AssetDatabase.RemoveObjectFromAsset(filter);
            AssetDatabase.SaveAssets();

            m_FiltersProp.DeleteArrayElementAtIndex(l.index);
            serializedObject.ApplyModifiedProperties();
        }

        void OnDrawFilter(Rect rect, int index, bool isActive, bool isFocused)
        {
            var filter = (AssetPoolFilter)m_FiltersProp.GetArrayElementAtIndex(index).objectReferenceValue;

            // Draw Header
            var mainLabelRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(mainLabelRect, filter.name, EditorStyles.boldLabel);
            rect.y += EditorGUIUtility.singleLineHeight;
            rect.height -= EditorGUIUtility.singleLineHeight;

            // Draw filter inspector
            EditorGUI.PropertyField(rect, m_FiltersProp.GetArrayElementAtIndex(index));

            serializedObject.ApplyModifiedProperties();
        }

        float OnFilterElementHeight(int index)
        {
            if (index >= m_FiltersProp.arraySize) { return 0; }

            var prop = m_FiltersProp.GetArrayElementAtIndex(index);
            // Add one extra line height for the filter name
            return prop != null ? EditorGUI.GetPropertyHeight(prop) + EditorGUIUtility.singleLineHeight : 0;
        }

        void ShowAddFilterDropdown(Rect buttonRect, ReorderableList l)
        {
            m_FiltersAddMenu.ShowAsContext();
        }

        void OnAddFilterClick(object rawType)
        {
            var type = (Type)rawType;
            var filter = (AssetPoolFilter)CreateInstance(type);
            filter.name = type.Name;

            var index = m_FiltersProp.arraySize;

            AssetDatabase.AddObjectToAsset(filter, (SyntheticHumanAssetPool)target);
            AssetDatabase.SaveAssets();

            m_FiltersProp.InsertArrayElementAtIndex(m_FiltersProp.arraySize);
            m_FiltersProp.GetArrayElementAtIndex(index).objectReferenceValue = filter;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
