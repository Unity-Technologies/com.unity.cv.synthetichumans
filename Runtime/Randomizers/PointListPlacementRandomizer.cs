using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;
using Random = Unity.Mathematics.Random;

namespace Unity.CV.SyntheticHumans.Randomizers
{

    [Serializable]
    [AddRandomizerMenu("Synthetic Humans/Placement-  Pointlist Placement Randomizer")]
    public class PointListPlacementRandomizer : PlacementRandomizer
    {
        Random m_RandomGenerator;

        [Serializable]
        public struct PointListSettings
        {
            [Tooltip("The anchor positions at which the targeted objects should be placed.")]
            public List<GameObject> anchorPoints;
            public bool randomizeRotation;
            public Vector2 rotationRangeX;
            public Vector2 rotationRangeY;
            public Vector2 rotationRangeZ;

            public FloatParameter xTranslationShift;
            public FloatParameter yTranslationShift;
            public FloatParameter zTranslationShift;
        }

        public PointListSettings pointListSetting;

        protected override void OnIterationStart()
        {
            base.OnIterationStart();

            m_RandomGenerator.state = SamplerState.NextRandomState();

            var tags = tagManager.Query<PointListPlacementRandomizerTag>().ToList();

            if (pointListSetting.anchorPoints.Count == 0)
            {
                Debug.LogError($"No positions were provided for the {nameof(PointListPlacementRandomizer)}.");
                return;
            }

            if (tags.Count > pointListSetting.anchorPoints.Count)
            {
                Debug.LogWarning($"The number of anchor points provided for the {nameof(PointListPlacementRandomizer)} is smaller than the number of objects that need to placed using this Randomizer. Some anchor points will be reused.");
            }
            foreach (var tag in tags)
            {
                tag.transform.position = pointListSetting.anchorPoints[tags.IndexOf(tag)%pointListSetting.anchorPoints.Count].transform.position;

                if (pointListSetting.randomizeRotation)
                {
                    tag.transform.eulerAngles = new Vector3(m_RandomGenerator.NextFloat(pointListSetting.rotationRangeX.x, pointListSetting.rotationRangeX.y),
                                                            m_RandomGenerator.NextFloat(pointListSetting.rotationRangeY.x, pointListSetting.rotationRangeY.y),
                                                            m_RandomGenerator.NextFloat(pointListSetting.rotationRangeZ.x, pointListSetting.rotationRangeZ.y)
                    );

                }

                tag.transform.Translate( Vector3.left * pointListSetting.xTranslationShift.Sample() );
                tag.transform.Translate(Vector3.up * pointListSetting.yTranslationShift.Sample());
                tag.transform.Translate(Vector3.forward * pointListSetting.zTranslationShift.Sample());
            }
        }
    }
}
