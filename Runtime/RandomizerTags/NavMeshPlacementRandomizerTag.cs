using System.Collections.Generic;
using Unity.CV.SyntheticHumans.Placement;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;

namespace Unity.CV.SyntheticHumans.Randomizers
{
    public class NavMeshPlacementRandomizerTag : RandomizerTag
    {
        public List<AnimationPlacementGroup> animationPlacementGroups;

        static Mathematics.Random s_RandomGenerator;

        public virtual SyntheticHumanPlacer SamplePlacer()
        {
            if (s_RandomGenerator.state == 0)
                s_RandomGenerator.state = SamplerState.NextRandomState();

            var syntheticHumanAnimationRandomizerTag = gameObject.GetComponent<SyntheticHumanAnimationRandomizerTag>();
            if (syntheticHumanAnimationRandomizerTag == null)
            {
                Debug.LogWarning($"Missing SyntheticHumanAnimationRandomizerTag on game object {gameObject.name}");
                return null;
            }

            var animationTag = syntheticHumanAnimationRandomizerTag.selectedAnimationTag;
            if (animationTag == null)
            {
                Debug.LogWarning($"Selected animation tag is null on game object {gameObject.name}");
                return null;
            }

            var placers = new List<SyntheticHumanPlacer>();
            foreach (var group in animationPlacementGroups)
            {
                if (group != null && group.placer != null && group.animationTags.Contains(animationTag))
                {
                    placers.Add(group.placer);
                }
            }

            if (placers.Count == 0)
            {
                Debug.LogWarning($"Cannot find placers for animation tag {animationTag.name}", animationTag);
                return null;
            }

            if (placers.Count > 1)
            {
                Debug.LogWarning($"Found more than one placer for animation tag {animationTag.name}", animationTag);
                return placers[s_RandomGenerator.NextInt(placers.Count)];
            }
            return placers[0];
        }
    }
}
