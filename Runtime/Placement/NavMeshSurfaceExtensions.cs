using Unity.AI.Navigation;
using UnityEngine;

namespace Unity.CV.SyntheticHumans.Placement
{
    public static class NavMeshSurfaceExtensions
    {
        public static void UpdateNavMesh(this NavMeshSurface surface)
        {
            // Rebuild NavMesh surface each time because NavMeshSurface.UpdateNavMesh() method is asynchronous and
            // it is not guaranteed to be completed in the same frame
            // TODO: create a custom NavMesh update method if rebuilding slows the performance
            if (surface.navMeshData != null)
            {
                surface.RemoveData();
            }
            surface.BuildNavMesh();
        }

        public static Bounds GetBounds(this NavMeshSurface surface)
        {
            var bounds = surface.navMeshData.sourceBounds;
            var center = surface.transform.TransformPoint(bounds.center);
            return new Bounds(center, bounds.size);
        }
    }
}
