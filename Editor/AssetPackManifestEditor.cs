using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.CV.SyntheticHumans.Tags;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.CV.SyntheticHumans.Editor
{
    [CustomEditor(typeof(AssetPackManifest))]
    class AssetPackManifestEditor : UnityEditor.Editor
    {
        public override async void OnInspectorGUI()
        {
            try
            {
                EditorGUILayout.BeginVertical();
                GUILayout.Label("Asset Processing", EditorStyles.largeLabel);

                EditorGUILayout.Space(2f);

                //var procBehavior = serializedObject.FindProperty(nameof(AssetPackManifest.assetProcessingBehavior));
                // var prevBehavior = procBehavior.intValue;
                // EditorGUILayout.PropertyField(procBehavior,
                //     new GUIContent($"Asset Processing Behavior", $"Select the {nameof(AssetProcessingBehavior)} that will be applied to the assets included in this {nameof(AssetPackManifest)}. This includes import settings, pre, and post processing. \n\nBehaviors are defined in the {nameof(SyntheticHumanAssetImporter)} class.\n\nWhen a behavior other than {AssetProcessingBehavior.None} is chosen, import settings on assets cannot be changed manually and will be enforced by the behavior. Set this to {AssetProcessingBehavior.None} temporarily when debugging asset import settings."));
                //

                //MK: Disabled asset processing behaviour selection UI. Forcing default SyntheticHuman behaviour.
                /*

                var behaviorNames = new List<string>() {"None"};
                var processingBehaviors =  TypeCache.GetTypesDerivedFrom<ISyntheticHumanAssetProcessor>().Select(t => t.FullName).ToList();
                behaviorNames.AddRange(processingBehaviors);
                var previousBehaviorTypeName =  serializedObject.FindProperty(nameof(AssetPackManifest.assetProcessingBehaviorTypeName)).stringValue;
                var selectedIndex = string.IsNullOrEmpty(previousBehaviorTypeName) ? 0 : behaviorNames.IndexOf(previousBehaviorTypeName);
                selectedIndex = EditorGUILayout.Popup("Asset Processing Behavior", selectedIndex, behaviorNames.ToArray());
                serializedObject.FindProperty(nameof(AssetPackManifest.assetProcessingBehaviorTypeName)).stringValue = selectedIndex == 0 ? null : processingBehaviors[selectedIndex - 1];

                var newBehaviorTypeName = serializedObject.FindProperty(nameof(AssetPackManifest.assetProcessingBehaviorTypeName)).stringValue;
                if (newBehaviorTypeName != previousBehaviorTypeName && !string.IsNullOrEmpty(newBehaviorTypeName))
                {
                    if (((AssetPackManifest) serializedObject.targetObject).reprocessAssetsOnBehaviorChange)
                    {
                        serializedObject.ApplyModifiedProperties();
                        Debug.Log($"Asset Processing Behavior changed for {target.name}. Re-importing all included assets with new behavior...");
                        SyntheticHumanAssetImporter.ReimportAllFoldersAtPackLevel((AssetPackManifest) serializedObject.targetObject);
                        await ReselectInEditor(target);
                    }
                }


                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AssetPackManifest.reprocessAssetsOnBehaviorChange)),
                    new GUIContent($"Re-import Assets on Behavior Change", $"When enabled, changing the Asset Processing Behavior to any option other than None will cause a re-import of all assets included in this pack."));

                */

                if (GUILayout.Button(new GUIContent("Re-import Included Assets", $"Reimport all packable assets and synthetic human tags included in the folder hierarchy of this asset pack. This will also result in the pack being refreshed.")))
                {
                    SyntheticHumanAssetImporter.ReimportAllFoldersAtPackLevel((AssetPackManifest) serializedObject.targetObject);
                    await ReselectInEditor(target);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(30f);
                EditorGUILayout.BeginVertical();

                GUILayout.Label("Included Assets", EditorStyles.largeLabel);
                EditorGUILayout.Space(2f);

                if (GUILayout.Button(new GUIContent("Refresh pack", $"Rebuild this pack based on the {nameof(SyntheticHumanTag)}s found in its folder hierarchy.")))
                {
                    await AssetPackManager.RefreshPackFromRootFolder((AssetPackManifest) serializedObject.targetObject);
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AssetPackManifest.allActiveTags)),
                    new GUIContent($"All Included Tags", $"List of all {nameof(SyntheticHumanTag)}s under the root folder of this {nameof(AssetPackManifest)}."));

                serializedObject.ApplyModifiedProperties();
                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                //When async calls are used we get exceptions after we come back to executing the code. This however does not seem
                //to have any adverse effect on the functionality. So a try/catch should be good enough for now.
            }
        }

        static async Task ReselectInEditor(Object target)
        {
            //Whenever the list of tags in the pack is modified, we need to re-select the object in the editor so that the inspector view refreshes and shows the modified pack.
            EditorGUILayout.EndVertical();
            Selection.activeObject = null;
            await Task.Delay(20);
            Selection.activeObject = target;
            EditorGUILayout.BeginVertical();


            //TODO: There is an issue here where if the inspector is locked before the Refresh pack button is clicked, the changes to the pack are not reflected in the inspector UI.
            //It looks like there is no fix for this.
        }
    }
}
