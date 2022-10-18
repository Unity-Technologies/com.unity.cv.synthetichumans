using System;
using Unity.CV.SyntheticHumans.Tags;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;

namespace Unity.CV.SyntheticHumans.Randomizers
{
    [Serializable]
    [AddRandomizerMenu("Synthetic Humans/Synthetic Human Animation Randomizer")]
    public class SyntheticHumanAnimationRandomizer : Randomizer
    {
        const string k_ClipName = "PlayerIdle";
        const string k_StateName = "Base Layer.RandomState";

        UniformSampler m_Sampler = new UniformSampler();

        void RandomizeAnimation(SyntheticHumanAnimationRandomizerTag tag)
        {
            if (!tag.gameObject.activeInHierarchy)
                return;

            var animator = tag.gameObject.GetComponent<Animator>();
            animator.applyRootMotion = tag.applyRootMotion;

            var overrider = tag.animatorOverrideController;

            tag.SampleAnimationTag();

            if (overrider != null && tag.selectedAnimationTag)
            {
                overrider[k_ClipName] = (AnimationClip) tag.selectedAnimationTag.linkedAsset;
                animator.Play(k_StateName, 0, m_Sampler.Sample());

                // Unity won't update the animator until this frame is ready to render.
                // Force to update the animator and human poses in the same frame for the collision checking in the randomizers
                // The delta time must be greater than 0 to apply the root motion
                animator.Update(0.001f);
            }
        }

        protected override void OnIterationStart()
        {
            var taggedObjects = tagManager.Query<SyntheticHumanAnimationRandomizerTag>();
            foreach (var taggedObject in taggedObjects)
            {
                RandomizeAnimation(taggedObject);
            }
        }
    }
}
