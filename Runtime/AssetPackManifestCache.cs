using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.CV.SyntheticHumans
{
    [Serializable]
    internal class AssetPackManifestCache : ScriptableObject
    {
        [FormerlySerializedAs("pools")]
        public List<AssetPackManifest> packs = new List<AssetPackManifest>();
    }
}
