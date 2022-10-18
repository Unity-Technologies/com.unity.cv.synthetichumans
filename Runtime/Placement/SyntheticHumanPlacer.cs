using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CV.SyntheticHumans.Randomizers;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Scenarios;
using Object = UnityEngine.Object;

namespace Unity.CV.SyntheticHumans.Placement
{
    [Serializable]
    public abstract class SyntheticHumanPlacer
    {
        public NavMeshPlacementRandomizer placementRandomizer;

        protected UniformSampler m_AnimationTimeSampler = new UniformSampler();

        protected static Mathematics.Random s_RandomGenerator;

        public abstract string name { get; }

        public abstract bool Place(GameObject target);

        // Populate the value to the PlacementRandomizer variable if it is not assigned
        protected void SetupPlacementRandomizer()
        {
            if (placementRandomizer == null)
            {
                var scenario = Object.FindObjectOfType<ScenarioBase>();
                if (scenario == null)
                {
                    Debug.LogError("Missing perception scenario in the scene.");
                    return;
                }
                placementRandomizer = scenario.GetRandomizer<NavMeshPlacementRandomizer>();
            }
        }

        // Add NavMesh Obstacle component to human object
        protected static void AddNavMeshObstacle(GameObject human)
        {
            var obstacle = human.GetComponent<NavMeshObstacle>();
            if (obstacle == null)
            {
                obstacle = human.AddComponent<NavMeshObstacle>();
            }
            obstacle.shape = NavMeshObstacleShape.Capsule;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;
        }


        // Validation collisions and visibility after placing the human object
        protected bool PostValidation(GameObject target, Camera camera, IEnumerable<Collider> humanColliders, IEnumerable<Collider> allowedCollisions)
        {
            // Validate visibility
            var success = InCameraView(target.GetComponent<SkinnedMeshRenderer>().bounds, camera, true);
            if (!success)
            {
                Debug.LogWarning("Failed to place the human at a visible position", target);
                return false;
            }

            // Validate collisions
            success = ValidateCollisions(target, humanColliders, allowedCollisions);
            if (!success)
            {
                Debug.LogWarning("Failed to place the human at a collision-free position", target);
                return false;
            }

            return true;
        }

        // Validate if a point in the world space is in the Camera's view
        protected virtual bool InCameraView(Bounds bounds, Camera camera, bool validateVisibility = false)
        {
            var points = new Vector3[]
            {
                bounds.min,
                bounds.max,
                new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
            };
            foreach (var point in points)
            {
                var screenPoint = camera.WorldToScreenPoint(point);
                if (screenPoint.z <= 0 || screenPoint.x < 0 || screenPoint.x >= camera.pixelWidth
                    || screenPoint.y < 0 || screenPoint.y >= camera.pixelHeight)
                    continue;

                if (!validateVisibility)
                    return true;

                var hit = Physics.Linecast(camera.transform.position, point);
                if (!hit)
                {
                    return true;
                }
            }
            return false;
        }

        // Validate that the human has collisions with the target ground and no collisions with other objects
        // Return true if there is no collisions that are not in the list of allowed collisions
        protected static bool ValidateCollisions(GameObject target, IEnumerable<Collider> humanColliders, IEnumerable<Collider> allowedCollisions)
        {
            var colliderManager = target.GetComponent<HumanColliderManager>();
            if (colliderManager == null || colliderManager.Colliders == null || colliderManager.Colliders.Count == 0)
            {
                Debug.LogError($"Missing colliders on {target.name} object. Skipping the collision validation. " +
                               $"Please double-check the collider generation in {nameof(HumanGenerationConfig)} settings.");
                return true;
            }
            // TODO: Add further movement to the human to resolve unexpected collisions
            return !HumanColliderManager.HasCollision(target, humanColliders, allowedCollisions.ToHashSet());
        }
    }
}
