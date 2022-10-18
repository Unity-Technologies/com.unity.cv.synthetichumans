using UnityEngine.Perception.GroundTruth.DataModel;

namespace Unity.CV.SyntheticHumans.Labelers
{
    class Keypoint3dAnnotationDefinition : AnnotationDefinition
    {
        internal const string labelerDescription = "Produce 3D keypoint annotations for all visible humans";

        /// <inheritdoc/>
        public override string modelType => "type.unity.com/unity.solo.Keypoint3dAnnotationDefinition";

        /// <inheritdoc/>
        public override string description => labelerDescription;

        internal Keypoint3dAnnotationDefinition(string id) : base(id) { }
    }
}
