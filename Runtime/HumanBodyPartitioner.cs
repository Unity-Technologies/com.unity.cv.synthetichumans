using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CV.SyntheticHumans.Tags;
using UnityEngine;

namespace Unity.CV.SyntheticHumans
{
    public class MeshTopology
    {
        public SyntheticHumanAgeRange Age;
        public SyntheticHumanGender Gender;

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            var topology = (MeshTopology) obj;
            return Age == topology.Age && Gender == topology.Gender;
        }

        public override int GetHashCode()
        {
            return Tuple.Create(Age, Gender).GetHashCode();
        }
    }

    public static class HumanBodyPartitioner
    {
        internal struct MeshPartition
        {
            public List<int> Vertices;
            public List<int> Triangles;
        }

        private static Dictionary<MeshTopology, Dictionary<int, MeshPartition>> s_MeshPartitions =
            new Dictionary<MeshTopology, Dictionary<int, MeshPartition>>();

        public static Dictionary<int, Mesh> Partition(Mesh mesh, MeshTopology topology)
        {
            if (!s_MeshPartitions.ContainsKey(topology))
            {
                s_MeshPartitions[topology] = PartitionMeshByBones(mesh);
            }

            var subMeshes = new Dictionary<int, Mesh>();
            var vertices = mesh.vertices;
            foreach (var jointIndex in s_MeshPartitions[topology].Keys)
            {
                subMeshes[jointIndex] = new Mesh();
                subMeshes[jointIndex].vertices = s_MeshPartitions[topology][jointIndex].Vertices
                    .Select(vertexIndex => vertices[vertexIndex]).ToArray();
                subMeshes[jointIndex].triangles = s_MeshPartitions[topology][jointIndex].Triangles.ToArray();
            }
            return subMeshes;
        }

        internal static Dictionary<int, MeshPartition> GetMeshPartitions(MeshTopology topology, Mesh mesh)
        {
            if (!s_MeshPartitions.ContainsKey(topology))
                PartitionMeshByBones(mesh);
            return s_MeshPartitions[topology];
        }

        static Dictionary<int, MeshPartition> PartitionMeshByBones(Mesh mesh)
        {
            var partitions = new Dictionary<int, MeshPartition>();
            var boneWeights = mesh.boneWeights;

            // Partition vertices into an array where the index of the element is the vertex index,
            // and the value of the element is the index of bone
            var vertexBone = new int[boneWeights.Length];
            for (var vertexIndex = 0; vertexIndex < boneWeights.Length; vertexIndex++)
            {
                var weights = new List<float>()
                {
                    boneWeights[vertexIndex].weight0, boneWeights[vertexIndex].weight1,
                    boneWeights[vertexIndex].weight2, boneWeights[vertexIndex].weight3
                };
                var boneIndices = new[]
                {
                    boneWeights[vertexIndex].boneIndex0, boneWeights[vertexIndex].boneIndex1,
                    boneWeights[vertexIndex].boneIndex2, boneWeights[vertexIndex].boneIndex3
                };
                vertexBone[vertexIndex] = boneIndices[weights.IndexOf(weights.Max())];
            }

            // Reindex vertices in their partitioned mesh
            var boneVertices = vertexBone.Select((value, index) => new {value, index})
                .GroupBy(x => x.value, x => x.index)
                .ToDictionary(g => g.Key, g => g.ToList());
            var vertexIndexInPartitions = new int[boneWeights.Length];
            foreach (var vertices in boneVertices.Values)
            {
                for (var i = 0; i < vertices.Count; i++)
                {
                    vertexIndexInPartitions[vertices[i]] = i;
                }
            }

            // Partition triangular faces to each bone
            var triangles = mesh.triangles;
            var boneTriangles = new Dictionary<int, List<int>>();
            for (var i = 0; i < triangles.Length / 3; i++)
            {
                var boneIndex = vertexBone[triangles[3 * i]];
                if (vertexBone[triangles[3 * i + 1]] == boneIndex && vertexBone[triangles[3 * i + 2]] == boneIndex)
                {
                    if (!boneTriangles.ContainsKey(boneIndex))
                        boneTriangles[boneIndex] = new List<int>();
                    boneTriangles[boneIndex].Add(vertexIndexInPartitions[triangles[3 * i]]);
                    boneTriangles[boneIndex].Add(vertexIndexInPartitions[triangles[3 * i + 1]]);
                    boneTriangles[boneIndex].Add(vertexIndexInPartitions[triangles[3 * i + 2]]);
                }
            }

            // Construct partition results
            foreach (var boneIndex in boneVertices.Keys)
            {
                partitions[boneIndex] = new MeshPartition
                {
                    Vertices = boneVertices[boneIndex],
                    Triangles = boneTriangles[boneIndex],
                };
            }
            return partitions;
        }
    }
}
