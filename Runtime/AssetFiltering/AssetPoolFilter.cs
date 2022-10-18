using System;
using UnityEngine;
using Unity.CV.SyntheticHumans.Tags;

namespace Unity.CV.SyntheticHumans
{
    public abstract class AssetPoolFilter : ScriptableObject
    {
        public abstract bool ShouldIncludeAsset(SyntheticHumanTag tag);
    }
}
