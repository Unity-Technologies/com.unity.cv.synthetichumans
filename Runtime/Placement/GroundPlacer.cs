using System;
using System.Linq;
using Unity.AI.Navigation;
using Unity.CV.SyntheticHumans.Tags;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Perception.Randomization.Samplers;

namespace Unity.CV.SyntheticHumans.Placement
{
    [Serializable]
    public abstract class GroundPlacer : SyntheticHumanPlacer
    {
        [AnimationPlacementGroupSerializedField]
        public bool forceToTouchGround = true;

        public override bool Place(GameObject target)
        {
            SetupPlacementRandomizer();
            if (placementRandomizer == null)
                return false;

            if (NavMeshSurface.activeSurfaces.Count == 0)
                return false;
            if (s_RandomGenerator.state == 0)
                s_RandomGenerator.state = SamplerState.NextRandomState();

            // Only allow one active NavMesh surface in the iteration
            // The NavMeshSurface needs updated to carve newly placed NavMesh Obstacles
            if (NavMeshSurface.activeSurfaces.Count > 1)
                Debug.LogError("Found multiple active NavMesh Surfaces. Will only choose the first surface");
            var surface = NavMeshSurface.activeSurfaces[0];
            surface.UpdateNavMesh();

            if (placementRandomizer.cameras.Count == 0)
            {
                Debug.LogError("Missing cameras in the AnimationAndNavMeshPlacement randomizer");
                return false;
            }
            var camera = placementRandomizer.cameras[s_RandomGenerator.NextInt(placementRandomizer.cameras.Count)];

            var success = SamplePositionOnNavMesh(camera, surface, out var position);
            if (!success)
            {
                Debug.LogWarning("Failed to find a position on NavMesh to place a human", target);
                return false;
            }
            Transport(target, position, Quaternion.Euler(0, s_RandomGenerator.NextFloat(0, 360), 0));
            Physics.SyncTransforms();

            // Validate visibility and collisions
            var allowedCollisions = NavMeshPlacerTag.GetActivePlacerTags<GroundPlacerTag>()
                .Select(t => t.GetComponent<Collider>())
                .Where(c => c != null);
            success = PostValidation(target, camera, target.GetComponent<HumanColliderManager>().Colliders, allowedCollisions);
            if (success)
                AddNavMeshObstacle(target);
            return success;
        }

        protected abstract bool SamplePositionOnNavMesh(Camera camera, NavMeshSurface surface, out Vector3 position);

        protected bool SamplePositionOnNavMesh(Vector3 initialPosition, NavMeshSurface surface, out Vector3 position)
        {
            var surfaceBounds = surface.navMeshData.sourceBounds;
            var success = NavMesh.SamplePosition(initialPosition, out var hit, surfaceBounds.size.magnitude, NavMeshPlacerTag.GetAreaMask<GroundPlacerTag>());
            if (!success)
            {
                position = Vector3.zero;
                return false;
            }

            success = Physics.Raycast(hit.position, Vector3.down, out var hitInfo, surfaceBounds.size.y);
            position = success ? hitInfo.point : Vector3.zero;
            return success;
        }

        void Transport(GameObject target, Vector3 position, Quaternion rotation)
        {
            target.transform.rotation = rotation;
            var renderer = target.GetComponent<SkinnedMeshRenderer>();
            if (renderer == null)
            {
                target.transform.position = position;
                return;
            }
            var mesh = new Mesh();
            renderer.BakeMesh(mesh);

            var offsetY = 0f;
            if (forceToTouchGround)
            {
                var minY = renderer.transform.position.y + mesh.vertices.Select(v => v.y).Min();
                offsetY = target.transform.position.y - minY;
            }
            target.transform.position = new Vector3(position.x, position.y + offsetY, position.z);
        }

        // Validate if any part of the human is visible in the camera
        // Return true if any of the checking point is visible in the camera
        static bool ValidateVisibility(GameObject target, Camera camera)
        {
            var renderer = target.GetComponent<SkinnedMeshRenderer>();
            if (renderer == null)
                return true;
            var bounds = renderer.bounds;
            var points = new Vector3[]
            {
                bounds.center,
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

                var hit = Physics.Linecast(point, camera.transform.position, out var hitInfo);
                if (!hit || hitInfo.transform.position == target.transform.position)
                    return true;
            }
            return false;
        }
    }
}
