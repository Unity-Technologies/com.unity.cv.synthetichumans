using System;
using Unity.AI.Navigation;
using UnityEngine;

namespace Unity.CV.SyntheticHumans.Placement
{
    [Serializable]
    class BoundBasedGroundPlacer : GroundPlacer
    {
        public override string name => "Bound-Based Ground Placement";

        protected override bool SamplePositionOnNavMesh(Camera camera, NavMeshSurface surface, out Vector3 position)
        {
            var initialPosition = SamplePositionInCameraFrustumAndNavMeshSurfaceBounds(camera, surface.GetBounds());
            return SamplePositionOnNavMesh(initialPosition, surface, out position);
        }

        static Vector3 SamplePositionInCameraFrustumAndNavMeshSurfaceBounds(Camera camera, Bounds bounds)
        {
            var aabb = PlacerUtility.CalculateAABBBoundsForCameraFrustumAndNavMeshSurfaceBounds(camera, bounds);
            return new Vector3(
                s_RandomGenerator.NextFloat(aabb.min.x, aabb.max.x),
                s_RandomGenerator.NextFloat(aabb.min.y, aabb.max.y),
                s_RandomGenerator.NextFloat(aabb.min.z, aabb.max.z));
        }
    }

}
