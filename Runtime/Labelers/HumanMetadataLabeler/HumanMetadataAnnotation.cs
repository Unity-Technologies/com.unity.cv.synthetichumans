using System;
using System.Collections.Generic;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace Unity.CV.SyntheticHumans.Labelers
{
    /// <summary>
    /// Bounding boxes for all of the labeled objects in a capture
    /// </summary>
    [Serializable]
    class HumanMetadataAnnotation : Annotation
    {
        public override string modelType => "type.unity.com/unity.solo.HumanMetadataAnnotation";

        /// <summary>
        /// The bounding boxes recorded by the annotator
        /// </summary>
        public IList<SyntheticHumanMetadata> metadata { get; set; }

        public HumanMetadataAnnotation(AnnotationDefinition def, string sensorId, IList<SyntheticHumanMetadata> metadata)
            : base(def, sensorId)
        {
            this.metadata = metadata;
        }

        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            foreach (var data in metadata)
            {
                var nested = builder.AddNestedMessageToVector("metadata");
                data.ToMessage(nested);
            }
        }
    }
}
