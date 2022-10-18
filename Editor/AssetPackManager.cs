using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Assertions;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.CV.SyntheticHumans.Tags;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.CV.SyntheticHumans.Editor
{
    static class AssetPackManager
    {
        public static string GetPackRootFolder(AssetPackManifest pack) => Path.GetDirectoryName(AssetDatabase.GetAssetPath(pack));

        // TODO (CS): We should remove foldertags entirely once we figure out what to do with assets that are not
        // produced directly by the content pipeline (materials, textures, etc.)

        /// <summary>
        /// Recursively updates all tags in given AssetPackManifest's root folder using information from the folder tags
        /// and any .taginfos that are exported with each asset. Then, adds all valid tags to the pack's list
        /// of active tags. Foldertags will only be used if no .taginfo is found for an asset.
        /// </summary>
        public static async Task RefreshPackFromRootFolder(AssetPackManifest pack)
        {
            pack.allActiveTags.Clear();
            AssetDatabase.Refresh();

            // The calls to Start/StopAssetEditing allow us to batch our asset creation and make things way faster.
            AssetDatabase.StartAssetEditing();

            try
            {
                var rootFolder = GetPackRootFolder(pack);

                var childDirectories = Directory.GetDirectories(rootFolder, "*", 0);
                foreach (var dir in childDirectories)
                {
                    await RecursivelyUpdatePackFromChildFolder(pack, dir, null);
                }

                EditorUtility.SetDirty(pack);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                // Ensure that we always stop asset editing to prevent unity from hanging if we encounter errors.
                AssetDatabase.StopAssetEditing();

                //reselect the pack in the editor to reflect the new changes
                Selection.activeObject = null;
                await Task.Delay(20);
                Selection.activeObject = pack;

                //TODO: There is an issue here where if the inspector is locked before the Refresh Pack button is clicked, the changes to the pack are not reflected in the inspector UI.
                //It looks like there is no fix for this.
            }
        }

        static async Task RecursivelyUpdatePackFromChildFolder(AssetPackManifest pack, string folderPath, SyntheticHumanTag templateTag)
        {
            var newTemplateTag = AssetDatabase.LoadAssetAtPath<SyntheticHumanTag>(
                Path.Combine(folderPath, SyntheticHumanTag.FOLDER_TAG_IDENTIFIER + ".asset"));

            if (newTemplateTag != null)
            {
                var dirty = newTemplateTag.SetFieldsFromTemplate(templateTag);
                if (dirty) { EditorUtility.SetDirty(newTemplateTag); }

                templateTag = newTemplateTag;
            }

            ProcessAllAssetsInFolder(pack, folderPath, templateTag);

            var childDirectories = Directory.GetDirectories(folderPath, "*", 0);
            foreach (var dir in childDirectories)
            {
                await RecursivelyUpdatePackFromChildFolder(pack, dir, templateTag);
            }
        }

        static void ProcessAllAssetsInFolder(AssetPackManifest pack, string folderPath, SyntheticHumanTag templateTag)
        {
            var assetsToProcess = new List<string>();
            var allFileInfos = new DirectoryInfo(folderPath).GetFiles("*.*");
            foreach (var fileInfo in allFileInfos)
            {
                var assetPath = Path.Combine(folderPath, fileInfo.Name);

                if (fileInfo.Name != SyntheticHumanTag.FOLDER_TAG_IDENTIFIER && IsPackableAsset(assetPath))
                {
                    assetsToProcess.Add(assetPath);
                }
            }

            foreach (var assetPath in assetsToProcess)
            {
                ProcessAsset(assetPath, pack, templateTag);
            }
        }

        static void ProcessAsset(string assetPath, AssetPackManifest pack, SyntheticHumanTag templateTag)
        {
            var maybeTaginfo = AssetDatabase.LoadAssetAtPath<SyntheticHumanTag>(
                Path.ChangeExtension(assetPath, ".taginfo"));

            // If an asset already has a taginfo file, we should just cast that to a tag and add it to the pack
            if (maybeTaginfo)
            {
                pack.allActiveTags.Add(maybeTaginfo);
            }

            // Otherwise, if we have a template tag, we should create a new tag for the object that matches
            // the template
            else if (templateTag != null)
            {
                var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                var newTag = Object.Instantiate(templateTag);

                newTag.linkedAsset = asset;
                newTag.name = Path.GetFileNameWithoutExtension(assetPath);

                AssetDatabase.CreateAsset(newTag, Path.ChangeExtension(assetPath, ".asset"));
                pack.allActiveTags.Add(newTag);
            }
        }

        /// <summary>
        /// Checks if an asset is valid to have its tag added to the asset pack.
        /// </summary>
        /// <param name="assetPath">The path to the asset</param>
        /// <returns>Returns true if this asset is valid</returns>
        public static bool IsPackableAsset(string assetPath)
        {
            var extension = Path.GetExtension(assetPath);

            if (extension == ".meta" || Directory.Exists(assetPath)) { return false; }
            //meta files and directories are not packable assets
            // We don't consider tags to be valid - only the assets they're related to.
            // If there is a asset pack situated under the root folder of another asset pack, we skip importing it as it may cause an infinite loop and is not supported.
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            try
            {
                return !assetType.IsSubclassOf(typeof(SyntheticHumanTag)) && assetType != typeof(SyntheticHumanTag) && assetType != typeof(AssetPackManifest) && assetType != typeof(AssetPackManifestCache);
            }
            catch (Exception e)
            {
                return false;
            }
        }


        /// <summary>
        /// 1. Remove SyntheticHumanTags of assets that were moved out of the pack scope or completely deleted.
        /// 2. Add new SyntheticHumanTags if they were imported into project or moved into a location that is in scope for the pack.
        ///
        /// If called from the native AssetPostprocessor.AssetOnPostprocessAllAssets, the moved and imported asset info can be used for improved updating and cleanup.
        /// Otherwise, we just remove the tags for which the linked asset does not exist anymore.
        /// </summary>
        /// <param name="pack">The pack to clean up</param>
        /// <param name="importedAssets">Paths of assets that were imported in the last asset processing job.</param>
        /// <param name="movedAssets">New paths of assets that were moved in the last asset processing job.</param>
        /// <param name="movedFromAssetPaths">Old paths of assets that were moved in the last asset processing job.</param>
        public static void UpdatePack(AssetPackManifest pack, string[] importedAssets = null, string[] movedAssets = null, string[] movedFromAssetPaths = null)
        {
            //Here we need to take care of both moved/imported packable assets and moved/imported synthetic human tags. Hence it is somewhat
            var tagsToRemove = new List<SyntheticHumanTag>();
            foreach (var syntheticHumanTag in pack.allActiveTags)
            {
                if (!(syntheticHumanTag && syntheticHumanTag.linkedAsset))
                {
                    //if the taginfo file or the asset file were removed
                    tagsToRemove.Add(syntheticHumanTag);
                }
            }

            AssetDatabase.StartAssetEditing();

            var packWasModified = tagsToRemove.Count > 0;

            pack.allActiveTags.RemoveAll(tag => tagsToRemove.Contains(tag));

            var packPath = AssetDatabase.GetAssetOrScenePath(pack);

            if (movedAssets != null && movedFromAssetPaths != null)
            {
                for (var i = 0; i < movedAssets.Length; i++)
                {
                    if (Directory.Exists(movedAssets[i]))
                        continue; //We don't need to check directories

                    if (IsPackableAsset(movedAssets[i]))
                    {
                        if (AssetBelongsToPack(movedAssets[i], packPath))
                        {
                            //in this new location asset belongs to this pack
                            if (AssetBelongsToPack(movedFromAssetPaths[i], packPath))
                            {
                                //asset used to belong to this pack at previous location too
                                if (Path.GetDirectoryName(movedAssets[i]) == Path.GetDirectoryName(movedFromAssetPaths[i]))
                                {
                                    //asset was just renamed, should not break anything and we don't need to do anything
                                }
                                else
                                {
                                    //Asset was moved to another location still inside the pack. If the SyntheticHumanTag was also moved with it we are good, but if not,
                                    //there is now a SyntheticHumanTag in the pack that is linked to an asset that's not in the same folder as the tag.
                                    //At this point we cannot tell which has happened because in some cases the tagfile and the associated assets go through two
                                    //separate import cycles, meaning the path to the tag is usually not in the movedAssets list we have here. We also cannot
                                    //load it from disk because if the tag is going through import, it will not be known to the asset database at its final location yet.
                                    //So we do nothing.
                                    //We assume both tag and linked asset were moved together, which means the reference in the tag pack is still good and should work.
                                    //I'm leaving this if/else block in for the explanation comments. Maybe we decide to do sth with them in the future.
                                }
                            }
                            else
                            {
                                //Asset did not belong to this pack at previous location but now it does belong. No need to do anything here, the asset's tag will be added to the pack a few lines below.
                            }
                        }
                        else
                        {
                            //in this new location, the asset does not belong to this pack
                            if (AssetBelongsToPack(movedFromAssetPaths[i], packPath))
                            {
                                //but asset used to belong to this pack at previous location, so there is a synthetic human tag in the pack that has a reference to the asset

                                packWasModified |= RemoveSyntheticHumanTagOfAssetFromPack(pack, movedAssets[i]);
                            }
                        }
                    }
                    else
                    {
                        var assetType = AssetDatabase.GetMainAssetTypeAtPath(movedAssets[i]);
                        if (typeof(SyntheticHumanTag).IsAssignableFrom(assetType))
                        {
                            if (AssetBelongsToPack(movedAssets[i], packPath))
                            {
                                //in this new location the synthetic human tag belongs to this pack
                                if (AssetBelongsToPack(movedFromAssetPaths[i], packPath))
                                {
                                    //synthetic human tag used to belong to this pack at previous location too
                                    if (Path.GetDirectoryName(movedAssets[i]) == Path.GetDirectoryName(movedFromAssetPaths[i]))
                                    {
                                        //synthetic human was just renamed, should not break anything and we don't need to do anything
                                    }
                                    else
                                    {
                                        //Synthetic human tag was moved to another location inside the pack. We have the same issue explained above for when packable assets are moved.
                                        //We can't do much at this point because we don't know if the asset linked to this tag was moved to the new place too or not.
                                        //We assume both tag and linked asset were moved together, which means the reference in the tag pack is still good and should work.
                                        //I'm leaving this if/else block in for the explanation comments. Maybe we decide to do sth with them in the future.
                                    }
                                }
                                else
                                {
                                    //synthetic human tag did not belong to pack previously, but does belong now
                                    packWasModified |= AddAssetToPackIfEligible(pack, movedAssets[i], packPath);
                                }
                            }
                        }
                    }
                }
            }

            if (importedAssets != null)
            {
                foreach (var path in importedAssets)
                {
                    if (Directory.Exists(path))
                        continue; //We don't need to check directories

                    packWasModified |= AddAssetToPackIfEligible(pack, path, packPath);
                }
            }

            if (packWasModified)
            {
                EditorUtility.SetDirty(pack);
                AssetDatabase.SaveAssets();
            }

            AssetDatabase.StopAssetEditing();
        }

        static bool AddAssetToPackIfEligible(AssetPackManifest pack, string assetPath, string packPath = null)
        {
            packPath ??= AssetDatabase.GetAssetPath(pack);

            var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if (assetType != null && typeof(SyntheticHumanTag).IsAssignableFrom(assetType) && AssetBelongsToPack(assetPath, packPath) && !Path.GetFileNameWithoutExtension(assetPath).EndsWith(SyntheticHumanTag.FOLDER_TAG_IDENTIFIER))
            {
                var tag = AssetDatabase.LoadAssetAtPath<SyntheticHumanTag>(assetPath);
                if (!pack.allActiveTags.Contains(tag))
                {
                    pack.allActiveTags.Add(tag);
                    return true;
                }
            }
            return false;
        }
        static bool RemoveSyntheticHumanTagOfAssetFromPack(AssetPackManifest pack, string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

            var alreadyExistingTag = pack.allActiveTags.FirstOrDefault(tag => tag.linkedAsset == asset);
            if (alreadyExistingTag)
            {
                pack.allActiveTags.Remove(alreadyExistingTag);
                return true;
            }

            return false;
        }

        public static bool AssetBelongsToPack(string assetPath, string packPath)
        {
            var packDir = Path.GetDirectoryName(packPath);
            var assetDir = Path.GetDirectoryName(assetPath);
            //asset is inside a folder hierarchy rooted at packRoot and also not directly inside packRoot
            return packDir != null && assetDir != null && assetDir.StartsWith(packDir+Path.DirectorySeparatorChar);
        }
    }
}
