using System;
using Unity.CV.SyntheticHumans.Tags;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;

namespace Unity.CV.SyntheticHumans.Randomizers
{
    [Serializable]
    [AddRandomizerMenu("Perception/Blend Shape Randomizer")]
    public class BlendShapeRandomizer : Randomizer
    {
        public FloatParameter m_blendShapeWeightParameter = new FloatParameter { value = new UniformSampler(0, 100) };
        Mesh m_skinnedMesh;
        int m_blendShapeCount;

        [Tooltip("The blend shapes added by this Randomizer.")]
        public GameObject[] newBlendShapes;

        /// <summary>
        /// Adds new blendshapes to tagged meshes if randomizer is fed meshes with or without blendshapes in Scenario
        /// </summary>
        protected override void OnScenarioStart()
        {
            if (newBlendShapes.Length > 0)
            {
                var tags = tagManager.Query<BlendShapeRandomizerTag>();

                foreach (var newBlendShape in newBlendShapes)
                {
                    foreach (var tag in tags)
                    {
                        //Randomizer tag's AddNewBlendShape()
                        tag.AddNewBlendShape(newBlendShape);
                    }
                }
            }
        }


        /// <summary>
        /// Randomizes the blendshapes inside of tagged meshes
        /// </summary>
        protected override void OnIterationStart()
        {
            var tags = tagManager.Query<BlendShapeRandomizerTag>();

            foreach (var tag in tags)
            {
                m_skinnedMesh = tag.GetComponent<SkinnedMeshRenderer>().sharedMesh;

                m_blendShapeCount = m_skinnedMesh.blendShapeCount;

                //Go through all the blendshapes of the mesh and apply a random weight
                for (int i = 0; i < m_blendShapeCount; i++)
                {
                    //Randomizer tag's SetWeight(int m_blendShapeIndex, float m_rawWeight) to set blend shape weight
                    tag.SetWeight(i, m_blendShapeWeightParameter.Sample());
                }
            }
        }
    }
}
