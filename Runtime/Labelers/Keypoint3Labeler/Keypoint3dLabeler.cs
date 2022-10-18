using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Perception.GroundTruth.Sensors.Channels;
using UnityEngine.Perception.Settings;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Unity.CV.SyntheticHumans.Labelers
{
    [Serializable]
    public class Keypoint3dLabeler : CameraLabeler
    {
        /// <inheritdoc/>
        public override string description => Keypoint3dAnnotationDefinition.labelerDescription;

        public string annotationId = "keypoint3d";

        public override string labelerId => annotationId;

        /// <inheritdoc/>
        protected override bool supportsVisualization => false;

        Dictionary<int, (AsyncFuture<Annotation> annotation, List<Keypoint3dEntity> keypoints)> m_FrameKeypointData;
        AnnotationDefinition m_AnnotationDefinition;

        protected override void Setup()
        {
            if (PerceptionSettings.endpoint.GetType() != typeof(SoloEndpoint))
                Debug.LogWarning($"Found {PerceptionSettings.endpoint.GetType().Name} as the endpoint. Please use SoloEndpoint for the keypoint3d labeler");

            m_AnnotationDefinition = new Keypoint3dAnnotationDefinition(annotationId);
            DatasetCapture.RegisterAnnotationDefinition(m_AnnotationDefinition);

            m_FrameKeypointData = new Dictionary<int, (AsyncFuture<Annotation>, List<Keypoint3dEntity>)>();

            perceptionCamera.EnableChannel<InstanceIdChannel>();
            perceptionCamera.RenderedObjectInfosCalculated += OnRenderedObjectInfoReadback;
        }

        protected override void OnEndRendering(ScriptableRenderContext scriptableRenderContext)
        {
            // Find all active humans in the scene, regardless of whether they are visible
            var keypoints = new List<Keypoint3dEntity>();
            var allHumans = Object.FindObjectsOfType<SingleHumanSpecification>();
            foreach (var human in allHumans)
            {
                // Skip any humam that does not have a labeling component or is not active/enabled
                var labeling = human.GetComponent<Labeling>();
                if (labeling != null && human.isActiveAndEnabled)
                {
                    var entity = new Keypoint3dEntity(labeling.instanceId);
                    foreach (var joint in human.GetComponentsInChildren<JointLabel>())
                    {
                        entity.Add(joint);
                    }
                    keypoints.Add(entity);
                }
            }

            if (allHumans.Any())
            {
                m_FrameKeypointData[Time.frameCount] = (
                    perceptionCamera.SensorHandle.ReportAnnotationAsync(m_AnnotationDefinition),
                    keypoints);
            }
        }

        void OnRenderedObjectInfoReadback(int frameCount, NativeArray<RenderedObjectInfo> objectInfos, SceneHierarchyInformation hierarchyInfo)
        {
            if (!m_FrameKeypointData.TryGetValue(frameCount, out var frameKeypointData))
                return;
            m_FrameKeypointData.Remove(frameCount);

            // Write out the keypoints data
            var toReport = new Keypoint3dAnnotation(m_AnnotationDefinition, perceptionCamera.id, frameKeypointData.keypoints);
            frameKeypointData.annotation.Report(toReport);
        }
    }
}
