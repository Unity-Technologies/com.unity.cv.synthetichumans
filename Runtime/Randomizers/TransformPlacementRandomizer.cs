using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Serialization;

namespace Unity.CV.SyntheticHumans.Randomizers
{
    [Serializable]
    [AddRandomizerMenu("Synthetic Humans/Placement- Transform Randomizer")]
    public class TransformPlacementRandomizer : PlacementRandomizer
    {
        [Serializable]
        public struct TransformRandomizerSettings
        {
            public bool randomizePosition;
            public float volumeSizeX;
            public float volumeSizeY;
            public float volumeSizeZ;
            public Vector3 volumeCenter;
            public bool randomizeRotation;
            public Vector2 rotationRangeX;
            public Vector2 rotationRangeY;
            public Vector2 rotationRangeZ;
            public bool randomizeScale;
            public Vector2 sizeRange;

        }

        [FormerlySerializedAs("passThroughSettings")]
        public TransformRandomizerSettings randomizationSettings;

        protected override void OnIterationStart()
        {
            base.OnIterationStart();

            var targetObjects = tagManager.Query<TransformPlacementRandomizerTag>().ToList();

            foreach (var taggedHuman in targetObjects)
            {
                if (!taggedHuman.GetComponent<TransformRandomizerTag>())
                {
                    var newTag = taggedHuman.gameObject.AddComponent<TransformRandomizerTag>();

                    newTag.positionMode = TransformMethod.Absolute;
                    newTag.shouldRandomizePosition = randomizationSettings.randomizePosition;

                    newTag.position.x = new UniformSampler((randomizationSettings.volumeSizeX / -2f) + randomizationSettings.volumeCenter.x, (randomizationSettings.volumeSizeX / 2f) + randomizationSettings.volumeCenter.x);
                    newTag.position.y = new UniformSampler((randomizationSettings.volumeSizeY / -2f) + randomizationSettings.volumeCenter.y, (randomizationSettings.volumeSizeY / 2f) + randomizationSettings.volumeCenter.y);
                    newTag.position.z = new UniformSampler((randomizationSettings.volumeSizeZ / -2f) + randomizationSettings.volumeCenter.z, (randomizationSettings.volumeSizeZ / 2f) + randomizationSettings.volumeCenter.z);

                    newTag.shouldRandomizeRotation = randomizationSettings.randomizeRotation;
                    newTag.rotationMode = TransformMethod.Absolute;

                    newTag.rotation.x = new UniformSampler(randomizationSettings.rotationRangeX.x, randomizationSettings.rotationRangeX.y);
                    newTag.rotation.y = new UniformSampler(randomizationSettings.rotationRangeY.x, randomizationSettings.rotationRangeY.y);
                    newTag.rotation.z = new UniformSampler(randomizationSettings.rotationRangeZ.x, randomizationSettings.rotationRangeZ.y);

                    newTag.shouldRandomizeScale = randomizationSettings.randomizeScale;
                    newTag.scaleMode = TransformMethod.Absolute;
                    newTag.useUniformScale = true;
                    newTag.uniformScale.value = new UniformSampler(randomizationSettings.sizeRange.x, randomizationSettings.sizeRange.y);
                }
            }

            var targetTags = tagManager.Query<TransformRandomizerTag>().ToList();
            foreach (var tag in targetTags)
            {
                tag.Randomize();
            }
        }
    }
}
