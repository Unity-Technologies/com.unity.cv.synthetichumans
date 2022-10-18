using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.CV.SyntheticHumans.Editor;
using Unity.CV.SyntheticHumans.Tags;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Unity.CV.SyntheticHumans
{
    /// <summary>
    /// This class is responsible for pre-processing and post-processing assets included in the <see cref="Unity.CV.SyntheticHumans"/> package.
    /// Processing behavior can be customized for assets that are included in a <see cref="AssetPackManifest"/>. This is done through selecting an <see cref="ISyntheticHumanAssetProcessor"/> in the <see cref="AssetPackManifest"/> inspector UI.
    /// </summary>
    sealed class SyntheticHumanAssetImporter : AssetPostprocessor
    {
        static bool s_ShouldRefreshAssetPackManifests = true;
        const string k_AssetPackManifestCachePath = "Assets/Resources/AssetPackManifestCache.asset";
        static AssetPackManifestCache s_ManifestCache;

        public static AssetPackManifestCache assetPackManifestCache
        {
            get
            {
                if (s_ManifestCache)
                    return s_ManifestCache;

                s_ManifestCache = AssetDatabase.LoadAssetAtPath<AssetPackManifestCache>(k_AssetPackManifestCachePath);

                if (!s_ManifestCache)
                    Debug.LogWarning($"Could not find a {nameof(AssetPackManifestCache)}s from the path {k_AssetPackManifestCachePath}. This asset should always exist in the project. One will be created.");

                s_ManifestCache = ScriptableObject.CreateInstance<AssetPackManifestCache>();
                AssetDatabase.CreateAsset(s_ManifestCache, k_AssetPackManifestCachePath);

                return s_ManifestCache;
            }
        }

        void OnPreprocessAsset()
        {
            //No matter the type of asset imported, this event is called before the other preprocess functions. So we update the manifest cache here only.
            //Debug.Log($"Preprocess asset being called on {assetImporter.assetPath}");
            RefreshManifestCacheIfNeeded();
            InvokeForAllAssetPacks(MethodBase.GetCurrentMethod().Name, new object[] {assetImporter});
        }

        void OnPreprocessTexture()
        {
            //Debug.Log($"Preprocess texture being called on {assetImporter.assetPath}");
            InvokeForAllAssetPacks(MethodBase.GetCurrentMethod().Name, new object[] {assetImporter});
        }

        void OnPreprocessModel()
        {
            //Debug.Log($"Preprocess model being called on {assetImporter.assetPath}");
            InvokeForAllAssetPacks(MethodBase.GetCurrentMethod().Name, new object[] {assetImporter});
        }

        void OnPostprocessMaterial(Material material)
        {
            //Debug.Log($"Preprocess model being called on {assetImporter.assetPath}");
            InvokeForAllAssetPacks(MethodBase.GetCurrentMethod().Name, new object[] {material});
        }

        DefaultSyntheticHumanAssetProcessor m_Processor;

        void InvokeForAllAssetPacks(string eventName, object[] parameters = null)
        {
            //MK: I have tested the performance of this piece of code compared to directly calling the functions without reflection, and the outcome is nearly identical.
            //I think the overhead that the editor introduces is so much larger that the difference between reflection and direct invocation becomes negligible, so we should be fine with this approach

            //MK: temporarily disabling the pack based import behaviors

            m_Processor ??= new DefaultSyntheticHumanAssetProcessor();
            var method = typeof(DefaultSyntheticHumanAssetProcessor).GetMethod(eventName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null)
                method.Invoke(m_Processor, parameters);

            /*
            foreach (var pack in assetPackManifestCache.packs)
            {
                try
                {
                    if (pack.reprocessAssetsOnBehaviorChange && !string.IsNullOrEmpty(pack.assetProcessingBehaviorTypeName) && AssetPackManager.AssetBelongsToPack(assetImporter.assetPath, AssetDatabase.GetAssetPath(pack)))
                    {
                        var type = Type.GetType(pack.assetProcessingBehaviorTypeName);
                        if (type == null)
                            continue;

                        pack.assetProcessingBehaviorInstance ??= Activator.CreateInstance(type);

                        var method = type.GetMethod(eventName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        if (method != null)
                            method.Invoke(pack.assetProcessingBehaviorInstance, parameters);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }*/
        }

        static void RefreshManifestCacheIfNeeded(bool force = false)
        {
            //MK: temporarily disabling the pack based import behaviors
            return;
            if (s_ShouldRefreshAssetPackManifests || force)
            {
                var paths = new List<string>();
                s_ShouldRefreshAssetPackManifests = false;
                AssetDatabase.FindAssets("t:AssetPackManifest").ToList().ForEach(guid => paths.Add(AssetDatabase.GUIDToAssetPath(guid)));
                assetPackManifestCache.packs.Clear();

                foreach (var path in paths)
                {
                    var pack = AssetDatabase.LoadAssetAtPath<AssetPackManifest>(path);
                    assetPackManifestCache.packs.Add(pack);
                }

                var packsAtSameRootOrUnderOthers = assetPackManifestCache.packs.Where(pack =>
                    assetPackManifestCache.packs.Any(pack2 => pack2 != pack && pack.assetPackRootPath == pack2.assetPackRootPath ||
                        AssetPackManager.AssetBelongsToPack(pack.assetPackRootPath, pack2.assetPackRootPath))).ToList();

                if (packsAtSameRootOrUnderOthers.Count > 0)
                {
                    Debug.LogError($"Error in {nameof(AssetPackManifest)}s: Make sure only one {nameof(AssetPackManifest)} object exists in any folder, and no {nameof(AssetPackManifest)} is inside the folder hierarchy of another one. Problematic packs found:\n");

                    foreach (var pack in packsAtSameRootOrUnderOthers)
                        Debug.LogError($"{pack.name} at {pack.assetPackRootPath}");
                }

                //Debug.Log($"Asset Pack Cache Refreshed");

                EditorUtility.SetDirty(assetPackManifestCache);

                //Make sure not to call AssetDatabase.SaveAssets() here because it will cause a reimport of the cache file which will get us back here eventually and cause an infinite loop. The editor will eventually save the changes to disk, and even if does not it is no big deal.
            }
        }

        /// <summary>
        /// Recursively re-imports all folders that are in the same folder as the given pack path.
        /// </summary>
        /// <param name="packPath"></param>
        public static void ReimportAllFoldersAtPackLevel(string packPath)
        {
            var packDir = Path.GetDirectoryName(packPath);
            if (packDir == null)
                return;

            var childDirectories = Directory.GetDirectories(packDir, "*", 0);
            foreach (var dir in childDirectories)
            {
                AssetDatabase.ImportAsset(dir, ImportAssetOptions.ImportRecursive);
            }
        }

        public static void ReimportAllFoldersAtPackLevel(AssetPackManifest pack)
        {
            var path = AssetDatabase.GetAssetPath(pack);
            ReimportAllFoldersAtPackLevel(path);
        }

        /// <summary>
        /// Reviews a moved or newly imported asset.
        /// First output specifies whether packs should be updated after this review. Second output specifies whether the cache of packs should be updated after this review.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static async Task<(bool, bool)> ReviewMovedOrImportedAsset(string path)
        {
            var shouldUpdateManifestCache = false;

            var type = AssetDatabase.GetMainAssetTypeAtPath(path);

            if (AssetPackManager.IsPackableAsset(path) || typeof(SyntheticHumanTag).IsAssignableFrom(type))
            {
                return (true, false);
            }

            if (type == typeof(AssetPackManifest))
            {
                //an asset pack was added, re-imported, or moved
                shouldUpdateManifestCache = true;

                var pack = AssetDatabase.LoadAssetAtPath<AssetPackManifest>(path);
                var currentDir = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;

                if (pack.assetPackRootPath == currentDir)
                {
                    //If this was an imported pack (comes from ImportedAssets): The pack is in the ImportedAssets list and has the correct root directory saved inside. This was either a re-import of an existing pack or a duplication.
                    //If this was a moved pack (comes from MovedAssets): The pack was just renamed but is still in the same folder. We do nothing
                }
                else
                {
                    //If this was an imported pack (comes from Imported Assets): This is either a new pack or copied from somewhere else to here. Either way it needs to be updated.
                    //If this was a moved pack (comes from MovedAssets): The pack was moved to a different path. It needs to be updated.

                    pack.assetPackRootPath = currentDir;
                    await AssetPackManager.RefreshPackFromRootFolder(pack);
                    if (!string.IsNullOrEmpty(pack.assetProcessingBehaviorTypeName))
                        ReimportAllFoldersAtPackLevel(path);
                }
            }

            return (false, shouldUpdateManifestCache);
        }

        static async void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            //MK: temporarily disabling the pack based import behaviors and auto pack updates
            return;

            //RefreshManifestCacheIfNeeded();

            //Debug.Log($"On post process all assets");

            var shouldUpdatePacks = false;
            var shouldUpdateManifestCache = false;

            foreach (var path in importedAssets)
            {
                var (item1, item2) = await ReviewMovedOrImportedAsset(path);
                shouldUpdatePacks |= item1;
                shouldUpdateManifestCache |= item2;
            }

            foreach (var path in movedAssets)
            {
                var (item1, item2) = await ReviewMovedOrImportedAsset(path);
                shouldUpdatePacks |= item1;
                shouldUpdateManifestCache |= item2;
            }

            foreach (var path in deletedAssets)
            {
                if (AssetDatabase.GetMainAssetTypeAtPath(path) != typeof(AssetPackManifest))
                {
                    shouldUpdatePacks = true;
                    shouldUpdateManifestCache = true;
                }
            }

            //if a change was made to the asset packs during the past processing cycle, force update the pack cache
            if (shouldUpdateManifestCache)
                RefreshManifestCacheIfNeeded(true);

            //We now update the existing asset packs to remove stale synthetic human tags and add newly added ones.
            if (shouldUpdatePacks)
                UpdatePacks(importedAssets, movedAssets, movedFromAssetPaths);

            s_ShouldRefreshAssetPackManifests = true;
        }

        static void UpdatePacks(string[] importedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var pack in assetPackManifestCache.packs)
            {
                if (!pack)
                {
                    //MK: pack should never be null, but just in case Let's keep an eye on it.
                    Debug.LogError("Null pack manifest found while updating packs.");
                    continue;
                }

                AssetPackManager.UpdatePack(pack, importedAssets, movedAssets, movedFromAssetPaths);
            }
        }
    }
}
