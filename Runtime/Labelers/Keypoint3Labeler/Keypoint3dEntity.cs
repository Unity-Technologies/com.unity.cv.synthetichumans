using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace Unity.CV.SyntheticHumans.Labelers
{
    class Keypoint3dEntity : IMessageProducer
    {
        /// <summary>
        /// The instance id of the entity
        /// </summary>
        public uint instanceId;

        /// <summary>
        /// Array of all of the keypoints
        /// </summary>
        public List<Keypoint3dValue> keypoints;

        public Keypoint3dEntity(uint instanceId)
        {
            this.instanceId = instanceId;
            keypoints = new List<Keypoint3dValue>();
        }

        public void Add(JointLabel label)
        {
            keypoints.Add(new Keypoint3dValue
            {
                label = label.name,
                location = label.transform.position,
                orientation = label.transform.rotation,
            });
        }

        public void ToMessage(IMessageBuilder builder)
        {
            builder.AddUInt("instanceId", instanceId);
            foreach (var keypoint in keypoints)
            {
                var nested = builder.AddNestedMessageToVector("keypoints");
                keypoint.ToMessage(nested);
            }
        }
    }
}
