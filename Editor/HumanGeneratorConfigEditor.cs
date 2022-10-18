using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UIElements.Toggle;

namespace Unity.CV.SyntheticHumans.Editor
{
    [CustomEditor(typeof(HumanGenerationConfig))]
    class HumanGenerationConfigEditor : UnityEditor.Editor
    {
        const string k_UxmlPath = "Packages/com.unity.cv.synthetichumans/Editor/Uxml/HumanGenerationConfig.uxml";
        List<string> m_EnumEntryNamesToSkip = new()
        {
            "None"
        };

        VisualElement m_HumanPropsContainer;
        VisualElement m_GenerationSettings;

        public override VisualElement CreateInspectorGUI()
        {
            var targetType = typeof(HumanGenerationConfig);
            var rootElement = new VisualElement();
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UxmlPath);
            visualTree.CloneTree(rootElement);

            m_HumanPropsContainer = rootElement.Query<VisualElement>("human_properties");
            m_GenerationSettings = rootElement.Query<VisualElement>("generation_settings");

            //We use Unity's stock property UI for these properties. We just specify which visual element in the UI should contain them
            var membersForStockUiAndContainers = new Dictionary<MemberInfo, VisualElement>
            {
                {targetType.GetField(nameof(HumanGenerationConfig.basePrefab)), m_GenerationSettings},
                {targetType.GetField(nameof(HumanGenerationConfig.ageRange)), m_HumanPropsContainer},
                {targetType.GetField(nameof(HumanGenerationConfig.heightRange)), m_HumanPropsContainer},
                {targetType.GetField(nameof(HumanGenerationConfig.weightRange)), m_HumanPropsContainer},
                {targetType.GetField(nameof(HumanGenerationConfig.assetTagPool)), m_GenerationSettings},
                {targetType.GetField(nameof(HumanGenerationConfig.heightWeightSolver)), m_GenerationSettings},
                {targetType.GetField(nameof(HumanGenerationConfig.jointSelfOcclusionDistance)), m_GenerationSettings},
                {targetType.GetField(nameof(HumanGenerationConfig.enableColliderGeneration)), m_GenerationSettings},
                {targetType.GetField(nameof(HumanGenerationConfig.requiredClothingParameters)), m_GenerationSettings},
                {targetType.GetField(nameof(HumanGenerationConfig.preselectedGenerationAssetRefs)), m_GenerationSettings}
            };

            foreach (var (memberInfo, container) in membersForStockUiAndContainers)
            {
                var propField = new PropertyField(serializedObject.FindProperty(memberInfo.Name));
                var attributes = memberInfo.GetCustomAttributes(true).ToList();
                var tooltipAttribute = attributes.Find(att => att is TooltipAttribute);
                if (tooltipAttribute != null)
                    propField.tooltip = (tooltipAttribute as TooltipAttribute)?.tooltip;
                propField.Bind(serializedObject);
                propField.AddToClassList("inner_property_one");
                container.Add(propField);
            }

            //For these properties, we need to take a categorical input from the user. The possible choices comes from an enum.
            var fieldNamesForCategoricalInput = new List<(string, string)>
            {
                (nameof(HumanGenerationConfig.genders), "Sex"),
                (nameof(HumanGenerationConfig.ethnicities), "Ethnicity")
            };

            foreach (var (fieldName, text) in fieldNamesForCategoricalInput)
            {
                var fieldInfo = typeof(HumanGenerationConfig).GetField(fieldName);
                var enumType = fieldInfo.FieldType.GenericTypeArguments.Single();
                var listProperty = serializedObject.FindProperty(fieldInfo.Name);


                var label = new Label(text);
                label.AddToClassList("section_header");
                label.AddToClassList("inner_property_one");

                var attributes = fieldInfo.GetCustomAttributes(true).ToList();
                var tooltipAttribute = attributes.Find(att => att is TooltipAttribute);
                if (tooltipAttribute != null)
                    label.tooltip = (tooltipAttribute as TooltipAttribute)?.tooltip;

                m_HumanPropsContainer.Add(label);

                foreach (var enumEntryName in Enum.GetNames(enumType))
                {
                    if(m_EnumEntryNamesToSkip.Contains(enumEntryName))
                        continue;

                    var prop = new Toggle(enumEntryName)
                    {
                        value = IndexOfValueInArrayProperty(listProperty, (int) Enum.Parse(enumType,enumEntryName)) != -1
                    };
                    prop.RegisterValueChangedCallback(evt => UpdateTargetField(evt, fieldName, enumType));
                    prop.AddToClassList("inner_property_two");
                    m_HumanPropsContainer.Add(prop);
                }
            }
            return rootElement;
        }

        static int IndexOfValueInArrayProperty(SerializedProperty arrayProperty, int value)
        {
            for (var i = 0; i < arrayProperty.arraySize; i++)
            {
                if (arrayProperty.GetArrayElementAtIndex(i).intValue == value)
                    return i;
            }

            return -1;
        }
        void UpdateTargetField(ChangeEvent<bool> evt, string fieldName, Type enumType)
        {
            var targetListProperty = serializedObject.FindProperty(fieldName);
            if (evt.target is Toggle toggle)
            {
                var intVal = (int) Enum.Parse(enumType, toggle.label);
                var index = IndexOfValueInArrayProperty(targetListProperty, intVal);
                if (evt.newValue)
                {
                    if (index == -1)
                    {
                        targetListProperty.InsertArrayElementAtIndex(targetListProperty.arraySize);
                        targetListProperty.GetArrayElementAtIndex(targetListProperty.arraySize - 1).intValue = intVal;
                    }
                }
                else
                {
                    if (index != -1)
                    {
                        targetListProperty.DeleteArrayElementAtIndex(index);
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
