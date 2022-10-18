using System.Collections.Generic;
using UnityEngine;
using Unity.CV.SyntheticHumans.Tags;
using UnityEngine.Serialization;

namespace Unity.CV.SyntheticHumans
{
    [CreateAssetMenu(fileName = "New Asset Pack Manifest", menuName = "Synthetic Humans/Tags/AssetPackManifest")]
    public class AssetPackManifest : ScriptableObject
    {
        public bool reprocessAssetsOnBehaviorChange = true;
        public string assetProcessingBehaviorTypeName = string.Empty;
        public object assetProcessingBehaviorInstance = null;
        public List<SyntheticHumanTag> allActiveTags = new List<SyntheticHumanTag>();
        [FormerlySerializedAs("poolRootPath")]
        public string assetPackRootPath;
    }
}
