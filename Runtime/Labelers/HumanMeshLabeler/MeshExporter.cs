using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.CV.SyntheticHumans.Labelers
{
    class MeshExporter
    {
        Vector3[] m_Vertices;
        int[] m_Triangles;
        bool m_ExportTriangles;
        HumanMeshLabeler.Encoding m_Encoding;

        public MeshExporter(SkinnedMeshRenderer renderer, bool exportTriangles,
            HumanMeshLabeler.Encoding encoding = HumanMeshLabeler.Encoding.ASCII)
        {
            var mesh = new Mesh();
            renderer.BakeMesh(mesh);

            // Make a copy of vertices and triangles out of mesh to avoid allocating a new array in every access
            m_Vertices = mesh.vertices;
            if (exportTriangles)
            {
                m_ExportTriangles = true;
                m_Triangles = mesh.triangles;
            }

            m_Encoding = encoding;
        }

        public int GetVerticesCount() => m_Vertices == null ? 0 : m_Vertices.Length;

        public int GetTrianglesCount() => m_Triangles == null ? 0 : m_Triangles.Length / 3;

        // Export the mesh to a file
        public async Task Export(string filePath)
        {
            switch (m_Encoding)
            {
                case HumanMeshLabeler.Encoding.ASCII:
                    await ExportVertices(m_Vertices, filePath);
                    if (m_ExportTriangles)
                    {
                        await ExportFaces(m_Triangles, filePath);
                    }
                    break;
                case HumanMeshLabeler.Encoding.Byte:
                    ExportVerticesAsBytes(m_Vertices, filePath);
                    if (m_ExportTriangles)
                    {
                        ExportFacesAsBytes(m_Triangles, filePath);
                    }
                    break;
                default:
                    throw new ArgumentException($"Unsupported encoding: {m_Encoding}");
            }


        }

        // Export vertices to a file
        static async Task ExportVertices(Vector3[] vertices, string filePath)
        {
            using (var writer = new StreamWriter(filePath, append: false))
            {
                for (var i = 0; i < vertices.Length; i++)
                {
                    await writer.WriteLineAsync($"v {vertices[i].x} {vertices[i].y} {vertices[i].z}");
                }
            }
        }

        // Export vertices in bytes to a file
        static void ExportVerticesAsBytes(Vector3[] vertices, string filePath)
        {
            var floats = new float[3 * vertices.Length];
            for (var i = 0; i < vertices.Length; i++)
            {
                floats[i * 3] = vertices[i].x;
                floats[i * 3 + 1] = vertices[i].y;
                floats[i * 3 + 2] = vertices[i].z;
            }
            var bytes = new byte[4 * floats.Length];
            Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
            File.WriteAllBytes(filePath, bytes);
        }

        // Export and append triangles to a file
        static async Task ExportFaces(int[] triangles, string filePath)
        {
            using (var writer = new StreamWriter(filePath, append: true))
            {
                var triangleIndex = 0;
                while (triangleIndex < triangles.Length / 3)
                {
                    await writer.WriteLineAsync($"f {triangles[triangleIndex*3] + 1} {triangles[triangleIndex*3+1] + 1} {triangles[triangleIndex*3+2] + 1}");
                    triangleIndex++;
                }
            }
        }

        // Export and append triangles as bytes to a file
        static void ExportFacesAsBytes(int[] triangles, string filePath)
        {
            var bytes = new byte[4 * triangles.Length];
            Buffer.BlockCopy(triangles, 0, bytes, 0, bytes.Length);
            File.WriteAllBytes(filePath, bytes);
        }
    }
}
