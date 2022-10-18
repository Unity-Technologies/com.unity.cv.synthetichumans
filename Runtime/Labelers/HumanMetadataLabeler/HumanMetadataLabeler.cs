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
    public class HumanMetadataLabeler : CameraLabeler
    {
        /// <inheritdoc/>
        public override string description => "Writes all configuration and randomization data per human for each visible human in the frame";

        public string annotationId = "Human metadata";

        /// <inheritdoc/>
        protected override bool supportsVisualization => false;

        /// <inheritdoc/>
        public override string labelerId => annotationId;

        // A dictionary that stores all of the metadata for each human from that frame. Because rendering happens async,
        // we maintain this dictionary to lookup the necessary metadata when it is time to write it.
        private Dictionary<int, (AsyncFuture<Annotation> annotation, IList<SyntheticHumanMetadata> config)> m_SyntheticHumanMetadatas;

        // Annotation definition for the annotation output data
        private AnnotationDefinition m_HumanMetadataDefinition;

        public HumanMetadataLabeler() { }

        protected override void Setup()
        {
            if (PerceptionSettings.endpoint.GetType() != typeof(SoloEndpoint))
                Debug.LogWarning($"Found {PerceptionSettings.endpoint.GetType().Name} as the endpoint. Please use SoloEndpoint for the human metadata labeler");

            m_SyntheticHumanMetadatas = new Dictionary<int, (AsyncFuture<Annotation> annotation, IList<SyntheticHumanMetadata> config)>();

            m_HumanMetadataDefinition = new HumanMetadataAnnotationDefinition(annotationId);
            DatasetCapture.RegisterAnnotationDefinition(m_HumanMetadataDefinition);

            perceptionCamera.EnableChannel<InstanceIdChannel>();
            perceptionCamera.RenderedObjectInfosCalculated += OnRenderedObjectInfoCalculated;
        }

        protected override void OnBeginRendering(ScriptableRenderContext scriptableRenderContext)
        {
            // Find all enabled humans in the scene, regardless of whether they are visible and build a list of metadata.
            var syntheticHumanMetadatas = new List<SyntheticHumanMetadata>();
            var allHumans = Object.FindObjectsOfType<SingleHumanSpecification>();
            foreach (var human in allHumans)
            {
                // Skip any human that does not have a labeling component or is not active/enabled.
                var labeling = human.GetComponent<Labeling>();
                if (labeling != null && human.isActiveAndEnabled)
                {
                    var instanceId = labeling.instanceId;
                    var spec = human.GetComponent<SingleHumanSpecification>();
                    var assetRefs = human.GetComponent<SingleHumanGenerationAssetRefs>();
                    syntheticHumanMetadatas.Add(new SyntheticHumanMetadata()
                    {
                        instanceId = instanceId,

                        age = spec.age.ToString(),
                        height = spec.normalizedHeight.ToString(),
                        weight = spec.normalizedWeight.ToString(),
                        gender = spec.gender.ToString(),
                        ethnicity = spec.ethnicity.ToString(),

                        bodyMeshTag = assetRefs.bodyMeshTag.name,
                        hairMeshTag = assetRefs.hairMeshTag.name,
                        faceVatTag = assetRefs.faceVATTag.name,
                        primaryBlendVatTag = assetRefs.primaryBlendVATTag.name,
                        secondaryBlendVatTag = assetRefs.secondaryBlendVATTag.name,
                        bodyMaterialTag = assetRefs.bodyMatTag.name,
                        faceMaterialTag = assetRefs.faceMatTag.name,
                        eyeMaterialTag = assetRefs.eyeMatTag.name,
                        hairMaterialTag = assetRefs.hairMatTag.name,
                        clothingTags = assetRefs.clothingTags.Select(x => x.name).ToArray(),
                        clothingMaterialTags = assetRefs.clothingMatTags.Select(x => x.name).ToArray(),
                    });
                }
            }

            // Notify the perception camera that we will be writing this annotation.
            if (allHumans.Any())
            {
                m_SyntheticHumanMetadatas[Time.frameCount] = (
                    perceptionCamera.SensorHandle.ReportAnnotationAsync(m_HumanMetadataDefinition),
                    syntheticHumanMetadatas);
            }
        }

        void OnRenderedObjectInfoCalculated(int frameCount, NativeArray<RenderedObjectInfo> renderedObjectInfos, SceneHierarchyInformation hierarchyInfo)
        {
            // Pull the appropriate frame from the dictionary of frame metadata
            if (!m_SyntheticHumanMetadatas.TryGetValue(frameCount, out var syntheticHumanConfigs))
                return;
            m_SyntheticHumanMetadatas.Remove(frameCount);

            // Could also be done with a join, but this downselects the list of humans to only ones that got rendered
            var renderedConfigs = m_SyntheticHumanMetadatas.Where(x => renderedObjectInfos.Any(y => y.instanceId == x.Key)).ToList();

            // Write out the configuration
            var toReport = new HumanMetadataAnnotation(m_HumanMetadataDefinition, perceptionCamera.id, syntheticHumanConfigs.config);
            syntheticHumanConfigs.annotation.Report(toReport);
        }
    }
}
