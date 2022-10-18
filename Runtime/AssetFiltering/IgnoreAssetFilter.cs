using Unity.CV.SyntheticHumans.Tags;

namespace Unity.CV.SyntheticHumans
{
    public class IgnoreAssetFilter : AssetPoolFilter
    {
        public SyntheticHumanTag ignoredTag;

        public override bool ShouldIncludeAsset(SyntheticHumanTag tag)
        {
            return tag != ignoredTag;
        }
    }
}
