using System;
using System.Collections.Generic;
using Unity.CV.SyntheticHumans.Tags;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.CV.SyntheticHumans
{
    [CreateAssetMenu(fileName = "SyntheticHumanAssetPool", menuName = "Synthetic Humans/Asset Pool")]
    [Serializable]
    public class SyntheticHumanAssetPool : ScriptableObject
    {
        [FormerlySerializedAs("sourceMetadataPools")]
        public List<AssetPackManifest> sourceAssetPacks = new List<AssetPackManifest>();

        public List<TextAsset> skeletonFiles = new List<TextAsset>();

        // We have a custom editor for adding filters, so hide the default inspector component for them
        [HideInInspector]
        public List<AssetPoolFilter> assetPoolFilters = new List<AssetPoolFilter>();

        [HideInInspector]
        public List<BodyTag> filteredBodyTags = new List<BodyTag>();
        [HideInInspector]
        public List<VATTag> filteredBodyVatTags = new List<VATTag>();
        [HideInInspector]
        public List<VATTag> filteredFaceVatTags = new List<VATTag>();
        [HideInInspector]
        public List<VATTag> filteredClothingVatTags = new List<VATTag>();

        [HideInInspector]
        public List<HairTag> filteredHairTags = new List<HairTag>();

        [HideInInspector]
        public List<MaterialTag> filteredBodyMatTags = new List<MaterialTag>();
        [HideInInspector]
        public List<MaterialTag> filteredFaceMatTags = new List<MaterialTag>();
        [HideInInspector]
        public List<MaterialTag> filteredHairMatTags = new List<MaterialTag>();
        [HideInInspector]
        public List<MaterialTag> filteredEyeMatTags = new List<MaterialTag>();

        [HideInInspector]
        public List<ClothingTag> filteredClothingTags = new List<ClothingTag>();

        [HideInInspector]
        public List<MaterialTag> filteredClothingMatTags = new List<MaterialTag>();

        [HideInInspector]
        public List<AnimationTag> filteredAnimTags = new List<AnimationTag>();


        void ResetTagLists ()
        {
            filteredBodyTags = new List<BodyTag>();
            filteredHairTags = new List<HairTag>();
            filteredFaceVatTags = new List<VATTag>();
            filteredBodyVatTags = new List<VATTag>();
            filteredClothingVatTags = new List<VATTag>();
            filteredBodyMatTags = new List<MaterialTag>();
            filteredFaceMatTags = new List<MaterialTag>();
            filteredHairMatTags = new List<MaterialTag>();
            filteredEyeMatTags = new List<MaterialTag>();
            filteredClothingTags = new List<ClothingTag>();
            filteredClothingMatTags = new List<MaterialTag>();

            filteredAnimTags = new List<AnimationTag>();
        }

        public void RefreshAssets()
        {
            ResetTagLists();

            foreach (var pack in sourceAssetPacks)
            {
                RefreshFromSingleAssetPack(pack);
            }
        }

        void RefreshFromSingleAssetPack(AssetPackManifest pack)
        {
            foreach (var activeTag in pack.allActiveTags)
            {
                if (activeTag == null || activeTag.Equals(null))
                {
                    Debug.LogWarning($"Blank elements found in AssetPackManifest {pack.name}. Consider refreshing it");
                    continue;
                }

                if (!ShouldIncludeAsset(activeTag))
                {
                    continue;
                }

                if (activeTag.GetType() == typeof(BodyTag))
                {
                    filteredBodyTags.Add(activeTag as BodyTag);
                }
                else if (activeTag.GetType() == typeof(VATTag))
                {
                    var tempTag = activeTag as VATTag;

                    if (tempTag != null && tempTag.element == SyntheticHumanElement.Body)
                        filteredBodyVatTags.Add(activeTag as VATTag);

                    else if (tempTag != null && tempTag.element == SyntheticHumanElement.Head)
                        filteredFaceVatTags.Add(activeTag as VATTag);

                    // TODO: (LP) break these into a subclass
                    else if (tempTag != null)
                        filteredClothingVatTags.Add(activeTag as VATTag);
                }
                else if (activeTag.GetType() == typeof(MaterialTag))
                {
                    var tempTag = activeTag as MaterialTag;
                    if (tempTag != null)
                    {
                        switch (tempTag.humanElement)
                        {
                            case SyntheticHumanElement.Body:
                                filteredBodyMatTags.Add(activeTag as MaterialTag);
                                break;
                            case SyntheticHumanElement.Head:
                                filteredFaceMatTags.Add(activeTag as MaterialTag);
                                break;
                            case SyntheticHumanElement.Hair:
                                filteredHairMatTags.Add(activeTag as MaterialTag);
                                break;
                            case SyntheticHumanElement.Eye:
                                filteredEyeMatTags.Add(activeTag as MaterialTag);
                                break;
                            case SyntheticHumanElement.None:
                            default:
                            {
                                if (tempTag.clothingElement != SyntheticHumanClothingElement.None)
                                    filteredClothingMatTags.Add(activeTag as MaterialTag);
                                break;
                            }
                        }
                    }
                }
                else if (activeTag.GetType() == typeof(ClothingTag))
                {
                    filteredClothingTags.Add(activeTag as ClothingTag);
                }
                else if (activeTag.GetType() == typeof(HairTag))
                {
                    filteredHairTags.Add(activeTag as HairTag);
                }
                else if (activeTag.GetType() == typeof(AnimationTag))
                {
                    filteredAnimTags.Add(activeTag as AnimationTag);
                }
            }
        }

        bool ShouldIncludeAsset(SyntheticHumanTag tag)
        {
            foreach (var filter in assetPoolFilters)
            {
                if (!filter.ShouldIncludeAsset(tag))
                {
                    return false;
                }
            }

            return true;
        }

        List<SyntheticHumanTag> ReturnFilteredTags(List<SyntheticHumanTag> tagList, SyntheticHumanTag filterValueTag)
        {
            var returnableList = new List<SyntheticHumanTag>();
            // for each tag passed in
            foreach (var listedTag in tagList)
            {
                var isMatch = true;
                // Check if important values match. If not, discard
                foreach (var fieldInfo in listedTag.GetType().GetFields())
                {
                    var value = fieldInfo.GetValue(listedTag);
                    if (value != null)
                    {
                        if (value.ToString() != "None" && value.ToString() != "" && value != fieldInfo.GetValue(filterValueTag))
                        {
                            isMatch = false;
                        }
                    }
                }
                if (isMatch)
                    returnableList.Add(listedTag);
            }

            return returnableList;

        }
    }
}
