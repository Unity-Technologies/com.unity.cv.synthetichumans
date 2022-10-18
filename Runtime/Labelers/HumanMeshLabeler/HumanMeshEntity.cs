using System;
using System.IO;
using UnityEngine;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.Settings;

namespace Unity.CV.SyntheticHumans.Labelers
{
    class HumanMeshEntity : IMessageProducer
    {
        /// <summary>
        /// The instance id of the entity
        /// </summary>
        public uint instanceId;

        /// <summary>
        /// The relative file path of the mesh in the solo folder
        /// </summary>
        public string meshFilePath;

        /// <summary>
        /// The global position of the human
        /// </summary>
        public Vector3 location;

        /// <summary>
        /// The global orientation of the human
        /// </summary>
        public Quaternion orientation;


        /// The mesh object of the entity
        MeshExporter m_MeshExporter;
        MeshExportTaskManager m_TaskManager;

        static string s_OutputFolder = ((SoloEndpoint) PerceptionSettings.endpoint).currentPath;

        public HumanMeshEntity(uint instanceId, string meshFilePath, SkinnedMeshRenderer renderer,
            bool exportMeshTriangles, MeshExportTaskManager taskManager,
            HumanMeshLabeler.Encoding encoding = HumanMeshLabeler.Encoding.ASCII)
        {
            this.instanceId = instanceId;
            this.meshFilePath = meshFilePath;
            location = renderer.transform.position;
            orientation = renderer.transform.rotation;
            m_MeshExporter = new MeshExporter(renderer, exportMeshTriangles, encoding);
            m_TaskManager = taskManager;
        }

        public int GetVerticesCount() => m_MeshExporter.GetVerticesCount();

        public int GetTrianglesCount() => m_MeshExporter.GetTrianglesCount();

        public void ToMessage(IMessageBuilder builder)
        {
            try
            {
                var absoluteMeshFilePath = Path.Combine(s_OutputFolder, meshFilePath);
                Directory.CreateDirectory(Path.GetDirectoryName(absoluteMeshFilePath));
                m_TaskManager.Run(m_MeshExporter, absoluteMeshFilePath);
            }
            catch (Exception e)
            {
                // Skip the reporting if any exception happens
                Debug.LogError($"Failed to export mesh for instance {instanceId} to file {meshFilePath}\n{e}");
                return;
            }
            builder.AddUInt("instanceId", instanceId);
            builder.AddFloatArray("global_location", MessageBuilderUtils.ToFloatVector(location));
            builder.AddFloatArray("global_orientation", MessageBuilderUtils.ToFloatVector(orientation));
            builder.AddString("mesh_file_path", meshFilePath);
        }
    }
}
