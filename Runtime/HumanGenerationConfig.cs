using System;
using System.Collections.Generic;
using Unity.CV.SyntheticHumans.Tags;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Serialization;

namespace Unity.CV.SyntheticHumans
{
    [CreateAssetMenu(fileName = "SyntheticHumanGenerationConfig", menuName = "Synthetic Humans/Human Generation Config")]
    [Serializable]
    public class HumanGenerationConfig : ScriptableObject
    {
        [Tooltip("The Synthetic Human Asset Pool to use for generating humans.")]
        public SyntheticHumanAssetPool assetTagPool;
        [Tooltip("In relation to the Perception Keypoint Labeler: The depth at which the humans can occlude their own joints from the camera. If a joint is behind or inside the human model by more than this amount, it will be occluded.")]
        public float jointSelfOcclusionDistance;
        [Tooltip("The base prefab to use for generating humans. This can be used for adding any required components to the humans.")]
        public GameObject basePrefab;
        SyntheticHumanSkeletonTemplate m_SkeletonTemplate;

        [Tooltip("Enable to add colliders to generated humans. The colliders will be used in collision detection and collision-based placement.")]
        public bool enableColliderGeneration = true;

        [Tooltip("The age range for the generated humans, in years. Age will be picked randomly in the selected range. In the underlying logic, the selected age value will be mapped to one of our currently available age categories. These are:\n\nNewborn: 0-1 year old (0-12 months)\nToddler: 1-3\nChild1: 3-6\nChild2: 6-9\nPreteen: 9-13\nTeen: 13-20\nAdult: 20-65\nElderly: 65+.\n\nThese categories are inclusive of the range minimum and exclusive of the maximum, similar to how Perception sampler ranges work. To get humans generated with a specific age category, plug in the exact range numbers listed above into the sampler range.\n\nThe categories may become more granular as the package evolves.")]
        public IntegerParameter ageRange = new IntegerParameter {value = new UniformSampler(0, 100, true, 0, 100)};

        [Tooltip("The sexes of the generated humans.")]
        public List<SyntheticHumanGender> genders = new()
        {
            SyntheticHumanGender.Female,
            SyntheticHumanGender.Male
        };

        [Tooltip("The ethnicities of the generated humans.")]
        public List<SyntheticHumanEthnicity> ethnicities = new()
        {
            SyntheticHumanEthnicity.African,
            SyntheticHumanEthnicity.LatinAmerican,
            SyntheticHumanEthnicity.MiddleEastern,
            SyntheticHumanEthnicity.Asian,
            SyntheticHumanEthnicity.Caucasian
        };

        [Tooltip("The range of heights to use for the generated humans. 0 is shortest and 1 is tallest.")]
        public FloatParameter heightRange = new FloatParameter {value = new UniformSampler(0, 1, true, 0, 1)};

        [Tooltip("The range of weights to use for the generated humans. 0 is smallest and 1 is largest.")]
        public FloatParameter weightRange = new FloatParameter {value = new UniformSampler(0, 1, true, 0, 1)};

        [Tooltip("The method of blending shapes to achieve height/weight combinations. Normally this should be left on Additive.")]
        public SyntheticHumanHeightWeightSolver heightWeightSolver = SyntheticHumanHeightWeightSolver.Additive;

        [Tooltip("The clothing items that should be added to the generated humans.")]
        public List<ClothingParameters> requiredClothingParameters;

        [Tooltip("For debugging purposes: A predefined list of SyntheticHuman tags can be used to generate humans. These will override randomized specs defined elsewhere in this Human Generation Config.")]
        public SingleHumanGenerationAssetRefs preselectedGenerationAssetRefs;

        internal void Init()
        {
            var copyName = $"Copy of {nameof(assetTagPool)}";
            assetTagPool = Instantiate(assetTagPool);
            assetTagPool.name = copyName;

            //The assetTagPool assigned is a scriptable object. Create a copy so that the original is not changed after we do our processing.
            //TODO (MK): Store the runtime data of the asset pools (the filtered tag lists) in a separate place so that we do not need to change the asset pool scriptable objects, and thus wouldn't need to create these copies
            //Once that's done we also would not need to create copies of the configs in the human generation randomizer because the only reason we do that is to change the reference to the original asset pool to the copy

            assetTagPool.RefreshAssets();
            m_SkeletonTemplate = new SyntheticHumanSkeletonTemplate(assetTagPool.skeletonFiles, jointSelfOcclusionDistance);
        }

        internal GeneratedSkeletonInfo CreateSkeleton(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            return m_SkeletonTemplate.CreateSkeleton(skinnedMeshRenderer);
        }
    }
}
