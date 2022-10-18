using System.Linq;
using UnityEngine;
using Unity.CV.SyntheticHumans.Tags;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;

namespace Unity.CV.SyntheticHumans.Randomizers
{
    [RequireComponent(typeof(Animator))]
    public class SyntheticHumanAnimationRandomizerTag : RandomizerTag
    {
        [Tooltip("Will disregard animation tags in Animation Tags in favor of sourcing from Animation Tag Pool object")]
        public bool useGlobalAnimationPool;
        [Tooltip("The Synthetic Human Asset Pool that can be used as a global animation pool selection")]
        public SyntheticHumanAssetPool animationTagPool;

        public CategoricalParameter<AnimationTag> animationTags;

        public bool applyRootMotion = false;

        [HideInInspector]
        public AnimationTag selectedAnimationTag;

        AnimatorOverrideController m_Controller;

        public AnimatorOverrideController animatorOverrideController
        {
            get
            {
                if (m_Controller == null)
                {
                    var animator = gameObject.GetComponent<Animator>();
                    var runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("AnimationRandomizerController");
                    m_Controller = new AnimatorOverrideController(runtimeAnimatorController);
                    animator.runtimeAnimatorController = m_Controller;
                }

                return m_Controller;
            }
        }

        public AnimationTag SampleAnimationTagFromPool( SyntheticHumanAssetPool assetPool, SingleHumanSpecification humanSpecs)
        {
            var s_RandomGenerator = new Mathematics.Random();
            s_RandomGenerator.state = SamplerState.NextRandomState();

            var compatibleTags = assetPool.filteredAnimTags.Where(tag =>
                (tag.weight == humanSpecs.weightRange || tag.weight == SyntheticHumanWeightRange.None) &&
                (tag.gender == humanSpecs.gender || tag.gender == SyntheticHumanGender.Neutral) &&
                (tag.age == humanSpecs.age || tag.age == SyntheticHumanAgeRange.None)
                ).ToList();

            return compatibleTags.Count > 0 ? compatibleTags[s_RandomGenerator.NextInt(0, compatibleTags.Count)] : null;
        }

        public void SampleAnimationTag()
        {
            if (useGlobalAnimationPool)
                selectedAnimationTag =  SampleAnimationTagFromPool(animationTagPool, gameObject.GetComponent<SingleHumanSpecification>());
            else
                selectedAnimationTag = animationTags.Sample();
        }
    }
}
