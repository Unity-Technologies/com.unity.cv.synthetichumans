using UnityEngine.Perception.GroundTruth.DataModel;

namespace Unity.CV.SyntheticHumans.Labelers
{
    class HumanMeshAnnotationDefinition : AnnotationDefinition
    {
        internal const string labelerDescription = "Dump 3D Mesh for all visible humans";

        /// <inheritdoc/>
        public override string modelType => "type.unity.com/unity.solo.HumanMeshAnnotationDefinition";

        /// <inheritdoc/>
        public override string description => labelerDescription;

        internal HumanMeshAnnotationDefinition(string id) : base(id) { }
    }
}
