using System.Collections.Generic;
using Unity.CV.SyntheticHumans.Tags;
using UnityEngine;
using UnityEngine.Perception.Randomization.Samplers;

namespace Unity.CV.SyntheticHumans.Placement
{
    public class AnimationPlacementGroupSerializedFieldAttribute : PropertyAttribute { }

    [CreateAssetMenu(fileName = "NewAnimPlacer", menuName = "Synthetic Humans/AnimationPlacementGroup")]
    public class AnimationPlacementGroup : ScriptableObject
    {
        [SerializeReference]
        public SyntheticHumanPlacer placer;

        public List<AnimationTag> animationTags;

        static Mathematics.Random s_RandomGenerator;

        public AnimationTag Sample()
        {
            s_RandomGenerator.state = SamplerState.NextRandomState();
            if (animationTags.Count == 0)
            {
                Debug.LogError("Cannot sample AnimationPlacementPair from an empty list");
                return null;
            }
            return animationTags[s_RandomGenerator.NextInt(animationTags.Count)];
        }
    }
}
