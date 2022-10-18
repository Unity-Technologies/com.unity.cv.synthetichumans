using System.Collections.Generic;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace Unity.CV.SyntheticHumans.Labelers
{
    class HumanMeshAnnotation : Annotation
    {
        public override string modelType => "type.unity.com/unity.solo.HumanMeshAnnotation";

        List<HumanMeshEntity> m_HumanMeshEntities;
        CameraProjection m_CameraProjection;

        internal HumanMeshAnnotation(AnnotationDefinition def, string sensorId, List<HumanMeshEntity> entities, CameraProjection cameraProjection)
            : base(def, sensorId)
        {
            m_HumanMeshEntities = entities;
            m_CameraProjection = cameraProjection;
        }

        /// <inheritdoc/>
        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);

            var cameraNested = builder.AddNestedMessage("camera");
            m_CameraProjection.ToMessage(cameraNested);

            // SyntheticHumans use the same number of vertices and triangles for all human types
            var meshNested = builder.AddNestedMessage("mesh");
            meshNested.AddInt("vertices_per_human", m_HumanMeshEntities.Count > 0 ? m_HumanMeshEntities[0].GetVerticesCount() : 0);
            meshNested.AddInt("triangles_per_human", m_HumanMeshEntities.Count > 0 ? m_HumanMeshEntities[0].GetTrianglesCount() : 0);

            foreach (var entity in m_HumanMeshEntities)
            {
                var nested = meshNested.AddNestedMessageToVector("meshes");
                entity.ToMessage(nested);
            }
        }
    }
}
