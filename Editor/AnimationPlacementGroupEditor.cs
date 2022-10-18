using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.CV.SyntheticHumans.Placement;
using Unity.CV.SyntheticHumans.Tags;
using UnityEditor;
using UnityEngine;

namespace Unity.CV.SyntheticHumans.Editor
{
    [CustomEditor(typeof(AnimationPlacementGroup))]
    class AnimationPlacementGroupEditor : UnityEditor.Editor
    {
        static Dictionary<Type, SyntheticHumanPlacer> s_SyntheticHumanPlacers = new Dictionary<Type, SyntheticHumanPlacer>();

        const string k_PlacerPropertyName = "placer";
        const string k_AnimationTagsPropertyName = "animationTags";

        public override void OnInspectorGUI()
        {
            var animationPlacementGroup = (AnimationPlacementGroup) target;
            serializedObject.Update();

            // Display placer types in the popup list
            var placementTypes = GetPlacementTypes();
            var placerProperty = serializedObject.FindProperty(k_PlacerPropertyName);
            string placerName = null;
            if (!string.IsNullOrEmpty(placerProperty.managedReferenceFullTypename))
            {
                var typeInfo = placerProperty.managedReferenceFullTypename.Split(' ');
                var assemblyInfo = typeInfo[0];
                var classInfo = typeInfo[1];
                var placerType = Type.GetType($"{classInfo}, {assemblyInfo}");
                placerName = ((SyntheticHumanPlacer) placerProperty.managedReferenceValue).name;
                // Populate the deserialized placer to the dictionary in case Unity cleared the memory in recompiling or runtime
                s_SyntheticHumanPlacers[placerType] = (SyntheticHumanPlacer) placerProperty.managedReferenceValue;
            }
            var placementNames = new List<string>() {"None"};
            placementNames.AddRange(placementTypes.Select(t => GetSyntheticHumanPlacer(t).name));

            var selectedIndex = placerName == null ? 0 : placementNames.IndexOf(placerName);
            selectedIndex = EditorGUILayout.Popup("Synthetic Human Placer", selectedIndex, placementNames.ToArray());
            placerProperty.managedReferenceValue = selectedIndex == 0 ? null : GetSyntheticHumanPlacer(placementTypes[selectedIndex - 1]);

            // Display extra properties of the selected placer
            if (placerProperty.managedReferenceValue != null)
            {
                var fieldInfoCollection = TypeCache.GetFieldsWithAttribute<AnimationPlacementGroupSerializedFieldAttribute>();
                foreach (var fieldInfo in fieldInfoCollection)
                {
                    var instanceType = placerProperty.managedReferenceValue.GetType();
                    var declareType = fieldInfo.DeclaringType;
                    if (declareType != null && (instanceType.IsSubclassOf(declareType) || instanceType == declareType))
                    {
                        EditorGUILayout.PropertyField(placerProperty.FindPropertyRelative(fieldInfo.Name));
                    }
                }
            }

            // Display the list of animation tags
            var animationTagProperty = serializedObject.FindProperty(k_AnimationTagsPropertyName);
            EditorGUILayout.PropertyField(animationTagProperty, new GUIContent("Animation Tags"));

            // Button that helps to load all animations tags from a folder
            if (GUILayout.Button("Load All Animation Tags From Folder"))
            {
                var folder = EditorUtility.OpenFolderPanel("Load all animation tags from folder", Application.dataPath, "");
                if (!folder.StartsWith(Application.dataPath))
                {
                    Debug.LogError("The folder of animation tags must be in the Assets/ folder");
                }
                else
                {
                    var files = Directory.GetFiles(folder, "*.asset", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        var path = file.Substring(Application.dataPath.Length - 6);
                        var asset = AssetDatabase.LoadAssetAtPath<AnimationTag>(path);
                        if (asset != null)
                        {
                            animationPlacementGroup.animationTags.Add(asset);
                        }
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        static List<Type> GetPlacementTypes()
        {
            var collection = TypeCache.GetTypesDerivedFrom<SyntheticHumanPlacer>();
            var types = new List<Type>();
            foreach (var type in collection)
                if (!type.IsAbstract && !type.IsInterface)
                    types.Add(type);
            return types;
        }

        static SyntheticHumanPlacer GetSyntheticHumanPlacer(Type type)
        {
            if (!s_SyntheticHumanPlacers.ContainsKey(type))
            {
                s_SyntheticHumanPlacers[type] = (SyntheticHumanPlacer) Activator.CreateInstance(type);
            }
            return s_SyntheticHumanPlacers[type];
        }
    }
}
