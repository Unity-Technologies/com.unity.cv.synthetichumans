
using System.Collections.Generic;
using Unity.CV.SyntheticHumans.Tags;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.CV.SyntheticHumans
{
    /// <summary>
    /// * Holds high level filtering information required for the <see cref="Unity.CV.SyntheticHumans.Generators.HumanGenerator"/> to find assets for this human and assign them to a paired <see cref="SingleHumanGenerationAssetRefs"/>
    /// * These values will influence what assets are deemed valid
    /// </summary>
    public class SingleHumanSpecification : MonoBehaviour
    {
        [Tooltip("Defines which base shapes will be selectable for a human when filtered by SyntheticHumanAge\nCurrently split by groups that have significantly different silhouettes as a base shape")]
        public SyntheticHumanAgeRange age;
        [Tooltip( "The height of this human, normalized between 0-1 for short to tall.")]
        public float normalizedHeight;
        public SyntheticHumanHeightRange heightRange;
        [Tooltip( "The height of this human, normalized between 0-1 from lightweight to heavy.")]
        public float normalizedWeight;
        public SyntheticHumanWeightRange weightRange;
        [FormerlySerializedAs("sex")]
        [Tooltip( "Defines which base shapes will be selectable for a human when filtered by SyntheticHumanGender")]
        public SyntheticHumanGender gender;
        [Tooltip( "Defines which skin materials will be selectable for a human when filtered by SyntheticHumanEthnicity")]
        public SyntheticHumanEthnicity ethnicity;
        [Tooltip( "Defines how height and weight will be applied\n\nNone applies no blending\n\nDiscrete picks one of 9 shapes on a matrix of height/weight\n\nTargetBlend will blend against one of 9 shapes using a sampled percentage\n\nAdditive blends height and weight independently with a sampled percentage")]
        public SyntheticHumanHeightWeightSolver heightWeightSolver;
        [Tooltip( "A list of clothing categories to apply to a new human. If left blank, a default list will be generated")]
        public List<ClothingParameters> requiredClothing = new List<ClothingParameters>();
    }
}
