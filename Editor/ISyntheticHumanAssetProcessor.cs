using UnityEditor;
using UnityEngine;

namespace Unity.CV.SyntheticHumans
{
    interface ISyntheticHumanAssetProcessor
    {
        public void OnPreprocessTexture(AssetImporter assetImporter);
        public void OnPreprocessModel(AssetImporter assetImporter);
        public void OnPostprocessMaterial(Material material);
    }
}
