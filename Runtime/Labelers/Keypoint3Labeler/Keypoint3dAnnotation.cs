using System.Collections.Generic;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace Unity.CV.SyntheticHumans.Labelers
{
    class Keypoint3dAnnotation : Annotation
    {
        public override string modelType => "type.unity.com/unity.solo.Keypoint3dAnnotation";

        public IEnumerable<Keypoint3dEntity> keypointsData;

        internal Keypoint3dAnnotation(AnnotationDefinition def, string sensorId, List<Keypoint3dEntity> entities)
            : base(def, sensorId)
        {
            keypointsData = entities;
        }

        /// <inheritdoc/>
        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            foreach (var entity in keypointsData)
            {
                var nested = builder.AddNestedMessageToVector("keypoints");
                entity.ToMessage(nested);
            }
        }
    }
}
