using System;
using System.Linq;
using Unity.CV.SyntheticHumans.Tags;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;
using Random = Unity.Mathematics.Random;

namespace Unity.CV.SyntheticHumans.Randomizers
{

    [Serializable]
    [AddRandomizerMenu("Synthetic Humans/Placement-  Volume Projection Randomizer")]
    public class VolumeProjectionPlacementRandomizer : PlacementRandomizer
    {
        Random m_RandomGenerator;

        [Serializable]
        public struct VolumeProjectionSettings
        {
            [Tooltip("Number of times to attempt to place each human on a valid collision surface before it is deactivated .")]
            public int maxPlacementAttempts;
            public bool randomizeRotation;
            public Vector2 rotationRangeX;
            public Vector2 rotationRangeY;
            public Vector2 rotationRangeZ;
            public Vector3Parameter boundsSampler;
        }

        public IntegerParameter projectionVolumeSampler;

        public VolumeProjectionSettings volumeProjectionSetting;

        protected override void OnScenarioStart()
        {
            m_RandomGenerator = SamplerState.CreateGenerator();
        }

        protected override void OnIterationStart()
        {
            base.OnIterationStart();

            var taggedProjectors = tagManager.Query<PlacementVolumeProjectorTag>().ToList();
            if (taggedProjectors.Count == 0)
            {
                Debug.LogError($"No objects in the Scene have a {nameof(PlacementVolumeProjectorTag)} component added. At least one such object is needed for the {nameof(VolumeProjectionPlacementRandomizer)} to work.");
                return;
            }
            var targetObjects = tagManager.Query<VolumeProjectionPlacementRandomizerObjectTag>().ToList();

            foreach (var targetObject in targetObjects)
            {
                var sampledCollider = taggedProjectors[m_RandomGenerator.NextInt(0, taggedProjectors.Count)].GetComponent<Collider>();

                targetObject.transform.position = GetProjectedSpawnPoint(sampledCollider);

                if (volumeProjectionSetting.randomizeRotation)
                {
                    targetObject.transform.eulerAngles = new Vector3(m_RandomGenerator.NextFloat(volumeProjectionSetting.rotationRangeX.x, volumeProjectionSetting.rotationRangeX.y),
                                                            m_RandomGenerator.NextFloat(volumeProjectionSetting.rotationRangeY.x, volumeProjectionSetting.rotationRangeY.y),
                                                            m_RandomGenerator.NextFloat(volumeProjectionSetting.rotationRangeZ.x, volumeProjectionSetting.rotationRangeZ.y)
                    );
                }

            }
        }


        /// <summary>
        /// Returns a point that is projected from a volume to hit an object tagged with a PlacementValidSurfaceTag.
        /// </summary>
        public Vector3 GetProjectedSpawnPoint(Collider inputSpawnCollider)
        {
            Vector3 samplePoint;
            RaycastHit hitInfo;

            //float anglethreshold = 5f + m_PerceptionCamera.fieldOfView;

            for (int i = 0; i < volumeProjectionSetting.maxPlacementAttempts; i++)
            {

                samplePoint = GetBoundPoint(inputSpawnCollider, inputSpawnCollider.GetComponent<MeshFilter>().mesh);

                if (Physics.Raycast(samplePoint + Vector3.down, Vector3.down, out hitInfo))
                {

                    if (hitInfo.collider.gameObject.GetComponent<PlacementValidSurfaceTag>())
                    {
                        return hitInfo.point;
                        // //TODO: add in an offset for the height of the thing being spawned. Roughly compute from center, not bottom
                        // // Check if the point is roughly in the camera FOV
                        // float angleDifference = Vector3.Angle(m_PerceptionCamera.transform.forward, hitInfo.point - m_PerceptionCamera.transform.position);
                        //
                        // if (angleDifference < anglethreshold)
                        // {
                        //     return hitInfo.point;
                        // }
                    }
                }
            }

            Debug.Log("Max tries on a placement object have been exceeded");
            return Vector3.zero;
        }

        /// <summary>
        /// Returns a point within a collider's bounding box
        /// </summary>
        Vector3 GetBoundPoint(Collider inputCollider, Mesh inputMesh)
        {
            var colliderLocalScale = inputCollider.transform.localScale;

            volumeProjectionSetting.boundsSampler.x = new UniformSampler(inputMesh.bounds.size.x * -0.5f * colliderLocalScale.x, inputMesh.bounds.size.x * 0.5f * colliderLocalScale.x);
            volumeProjectionSetting.boundsSampler.y = new UniformSampler(inputMesh.bounds.size.y * -0.5f * colliderLocalScale.y, inputMesh.bounds.size.y * 0.5f * colliderLocalScale.y);
            volumeProjectionSetting.boundsSampler.x = new UniformSampler(inputMesh.bounds.size.z * -0.5f * colliderLocalScale.z, inputMesh.bounds.size.z * 0.5f * colliderLocalScale.z);

            var xsampler = new UniformSampler(inputMesh.bounds.size.x * -0.5f * colliderLocalScale.x, inputMesh.bounds.size.x * 0.5f * colliderLocalScale.x);
            var ysampler = new UniformSampler(inputMesh.bounds.size.y * -0.5f * colliderLocalScale.y, inputMesh.bounds.size.y * 0.5f * colliderLocalScale.y);
            var zsampler = new UniformSampler(inputMesh.bounds.size.z * -0.5f * colliderLocalScale.z, inputMesh.bounds.size.z * 0.5f * colliderLocalScale.z);

            //var samplePoint = volumeProjectionSetting.boundsSampler.Sample();
            var samplePoint = new Vector3(xsampler.Sample(), ysampler.Sample(), zsampler.Sample());

            return inputCollider.ClosestPoint(Quaternion.Euler(inputCollider.transform.eulerAngles) * samplePoint + inputCollider.gameObject.transform.position);
        }

    }



}
