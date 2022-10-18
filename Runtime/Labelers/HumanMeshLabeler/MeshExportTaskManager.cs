using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.CV.SyntheticHumans.Labelers
{
    class MeshExportTaskManager : MonoBehaviour
    {
        private Queue<Task> m_MeshExportTasks;

        public static MeshExportTaskManager GetOrCreate(GameObject parent)
        {
            var manager = parent.GetComponent<MeshExportTaskManager>();
            if (manager == null)
            {
                manager = parent.AddComponent<MeshExportTaskManager>();
                manager.m_MeshExportTasks = new Queue<Task>();
            }

            return manager;
        }

        public void Run(MeshExporter exporter, string meshFilePath)
        {
            var task = Task.Run(() => exporter.Export(meshFilePath));
            m_MeshExportTasks.Enqueue(task);
        }

        void Dequeue()
        {
            var task = m_MeshExportTasks.Dequeue();
            if (task.IsFaulted)
                Debug.LogError(task.Exception);
        }

        void Update()
        {
            // Remove the finished tasks to free memory in case the simulation takes a long time
            while (m_MeshExportTasks.Count > 0 && m_MeshExportTasks.Peek().IsCompleted)
                Dequeue();
        }

        void OnApplicationQuit()
        {
            var remainCount = m_MeshExportTasks.Count;
            while (m_MeshExportTasks.Count > 0)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayProgressBar("Waiting Mesh Exporting to Complete",
                    $"Exporting meshes: {remainCount - m_MeshExportTasks.Count} / {remainCount}",
                    (remainCount - m_MeshExportTasks.Count) / (float) remainCount);
#endif
                m_MeshExportTasks.Peek().Wait();
                Dequeue();
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
#endif
        }
    }
}
