using UnityEngine;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace Unity.CV.SyntheticHumans.Labelers
{
    class Keypoint3dValue : IMessageProducer
    {
        /// <summary>
        /// The label of the keypoint in the template file
        /// </summary>
        public string label;

        /// <summary>
        /// The location of the keypoint
        /// </summary>
        public Vector3 location;

        /// <summary>
        /// The orientation of the keypoint
        /// </summary>
        public Quaternion orientation;

        public void ToMessage(IMessageBuilder builder)
        {
            builder.AddString("label", label);
            builder.AddFloatArray("location", MessageBuilderUtils.ToFloatVector(location));
            builder.AddFloatArray("orientation", MessageBuilderUtils.ToFloatVector(orientation));
        }
    }
}
