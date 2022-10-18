using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.CV.SyntheticHumans
{
    [Serializable]
    class SkinWeights
    {
        [Serializable]
        public class VertexWeight
        {
            public string[] bones;
            public float[] weights;
        }

        public VertexWeight[] vertexWeights;

        /// <summary>
        /// Load weights to a mesh by reading from a properly formatted weightFile asset.
        /// </summary>
        public void ApplyWeightsToMesh(Mesh mesh, GeneratedSkeletonInfo skeletonInfo)
        {
            Assert.AreEqual(mesh.vertexCount, vertexWeights.Length,
                $"Vertex counts for mesh and weight file differ.");

            var outputWeights = new BoneWeight[mesh.vertexCount];
            for (var i = 0; i < vertexWeights.Length; i++)
            {
                var raw = vertexWeights[i];
                var weight = new BoneWeight();

                // TODO (CS): This is a hack introduced because the "root" bone at weight generation time is at the hips,
                // but the root bone at runtime is placed at the feet. We need a better fix for this.
                for (var j = 0; j < raw.bones.Length; j++)
                {
                    if (raw.bones[j] == "root")
                    {
                        raw.bones[j] = "hip";
                    }
                }

                weight.weight0 = raw.weights.Length > 0 ? raw.weights[0] : 0;
                weight.weight1 = raw.weights.Length > 1 ? raw.weights[1] : 0;
                weight.weight2 = raw.weights.Length > 2 ? raw.weights[2] : 0;
                weight.weight3 = raw.weights.Length > 3 ? raw.weights[3] : 0;
                weight.boneIndex0 = raw.bones.Length > 0 ? skeletonInfo.BoneNameIndices[raw.bones[0]] : 0;
                weight.boneIndex1 = raw.bones.Length > 1 ? skeletonInfo.BoneNameIndices[raw.bones[1]] : 0;
                weight.boneIndex2 = raw.bones.Length > 2 ? skeletonInfo.BoneNameIndices[raw.bones[2]] : 0;
                weight.boneIndex3 = raw.bones.Length > 3 ? skeletonInfo.BoneNameIndices[raw.bones[3]] : 0;

                // WARNING - unity expects these skin weights to add to exactly 1, otherwise it freaks out.
                // Apply an offset to the largest weight in order to reach exactly 1. Usually this remainder
                // is miniscule if the output data is clean.
                var remainder = 1.0f - (weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3);
                weight.weight0 += remainder;

                outputWeights[i] = weight;
            }

            mesh.boneWeights = outputWeights;
            mesh.bindposes = skeletonInfo.InitialPoses;
        }

        public static SkinWeights LoadFromSkinnedMeshRenderer(SkinnedMeshRenderer renderer)
        {
            var weights = new SkinWeights();
            var boneIndexNames = new Dictionary<int, string>();

            for (var i = 0; i < renderer.bones.Length; i++)
            {
                boneIndexNames[i] = renderer.bones[i].name;
            }

            var mesh = renderer.sharedMesh;
            weights.vertexWeights = new VertexWeight[mesh.boneWeights.Length];
            for (var i = 0; i < mesh.boneWeights.Length; i++)
            {
                var orig = mesh.boneWeights[i];
                weights.vertexWeights[i] = new VertexWeight
                {
                    bones = new string[] { boneIndexNames[orig.boneIndex0], boneIndexNames[orig.boneIndex1], boneIndexNames[orig.boneIndex2], boneIndexNames[orig.boneIndex3] },
                    weights = new float[] { orig.weight0, orig.weight1, orig.weight2, orig.weight3 }
                };
            }

            return weights;
        }

        /// <summary>
        /// Load weights to a mesh such that the entire mesh is bound to a single bone. Mostly useful for debugging.
        /// </summary>
        public static void GenerateWeightsForSingleBone(string boneName, Mesh mesh, GeneratedSkeletonInfo skeletonInfo)
        {
            var weights = new BoneWeight[mesh.vertexCount];

            for (var i = 0; i < weights.Length; i++)
            {
                weights[i] = new BoneWeight
                {
                    weight0 = 1,
                    boneIndex0 = skeletonInfo.BoneNameIndices[boneName]
                };
            }

            mesh.boneWeights = weights;
            mesh.bindposes = skeletonInfo.InitialPoses;
        }
    }
}
