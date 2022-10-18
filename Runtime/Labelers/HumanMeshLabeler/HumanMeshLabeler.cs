using System;
using System.Collections.Generic;
using System.IO;
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
    public class HumanMeshLabeler : CameraLabeler
    {
        public enum Encoding
        {
            ASCII,
            Byte,
        }

        /// <inheritdoc/>
        public override string description => HumanMeshAnnotationDefinition.labelerDescription;

        public string annotationId = "human mesh";

        public override string labelerId => annotationId;

        public bool exportMeshTriangles = false;

        public Encoding encoding = Encoding.ASCII;

        protected override bool supportsVisualization => false;

        Dictionary<int, (AsyncFuture<Annotation> annotation, List<HumanMeshEntity> meshEntities, CameraProjection cameraProjection)> m_FrameMeshData;
        AnnotationDefinition m_AnnotationDefinition;
        MeshExportTaskManager m_MeshExportTaskManager;

        IConsumerEndpoint m_ActiveEndpoint;

        const string k_SubFolder = "HumanMesh";

        protected override void Setup()
        {
            m_ActiveEndpoint = PerceptionSettings.endpoint;

            if (m_ActiveEndpoint.GetType() != typeof(SoloEndpoint))
            {
                Debug.LogError($"Found {PerceptionSettings.endpoint.GetType().Name} as the endpoint. Please use SoloEndpoint for the human mesh labeler");
                return;
            }

            m_AnnotationDefinition = new HumanMeshAnnotationDefinition(annotationId);
            m_MeshExportTaskManager = MeshExportTaskManager.GetOrCreate(perceptionCamera.gameObject);
            DatasetCapture.RegisterAnnotationDefinition(m_AnnotationDefinition);

            m_FrameMeshData = new Dictionary<int, (AsyncFuture<Annotation>, List<HumanMeshEntity>, CameraProjection)>();
            perceptionCamera.EnableChannel<InstanceIdChannel>();
            perceptionCamera.RenderedObjectInfosCalculated += OnRenderedObjectInfoReadback;
        }

        protected override void OnEndRendering(ScriptableRenderContext scriptableRenderContext)
        {
            if (m_ActiveEndpoint.GetType() != typeof(SoloEndpoint))
            {
                return;
            }

            // Find all active humans in the scene, regardless of whether they are visible
            var meshEntities = new List<HumanMeshEntity>();
            var allHumans = Object.FindObjectsOfType<SingleHumanSpecification>();
            foreach (var human in allHumans)
            {
                // Skip any human that does not have a labeling component or is not active/enabled
                var labeling = human.GetComponent<Labeling>();
                if (labeling != null && human.isActiveAndEnabled)
                {
                    meshEntities.Add(new HumanMeshEntity(labeling.instanceId,
                        Path.Combine(k_SubFolder, $"frame_{Time.frameCount}", $"{labeling.instanceId}.obj"),
                        human.GetComponent<SkinnedMeshRenderer>(), exportMeshTriangles, m_MeshExportTaskManager, encoding));
                }
            }

            if (allHumans.Any())
            {
                m_FrameMeshData[Time.frameCount] = (
                    perceptionCamera.SensorHandle.ReportAnnotationAsync(m_AnnotationDefinition),
                    meshEntities,
                    new CameraProjection(perceptionCamera.attachedCamera));
            }
        }

        void OnRenderedObjectInfoReadback(int frameCount, NativeArray<RenderedObjectInfo> objectInfos, SceneHierarchyInformation hierarchyInfo)
        {
            if (m_ActiveEndpoint.GetType() != typeof(SoloEndpoint))
            {
                return;
            }

            if (!m_FrameMeshData.TryGetValue(frameCount, out var frameMeshData))
                return;
            m_FrameMeshData.Remove(frameCount);

            // Write out the mesh data
            var toReport = new HumanMeshAnnotation(m_AnnotationDefinition, perceptionCamera.id, frameMeshData.meshEntities,
                frameMeshData.cameraProjection);
            frameMeshData.annotation.Report(toReport);
        }
    }
}
