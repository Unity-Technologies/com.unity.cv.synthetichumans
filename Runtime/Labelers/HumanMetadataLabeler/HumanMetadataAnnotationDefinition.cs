using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace Unity.CV.SyntheticHumans.Labelers
{
    class HumanMetadataAnnotationDefinition : AnnotationDefinition
    {
        internal const string labelerDescription = "Config information for each spawned human";

        /// <inheritdoc/>
        public override string modelType => "type.unity.com/unity.solo.HumanMetadataAnnotationDefinition";

        /// <inheritdoc/>
        public override string description => labelerDescription;

        internal HumanMetadataAnnotationDefinition(string id) : base(id) { }
    }
}
