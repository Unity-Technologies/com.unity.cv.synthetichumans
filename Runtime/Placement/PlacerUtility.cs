using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.CV.SyntheticHumans.Placement
{
    public static class PlacerUtility
    {
        // Calculate the AABB bounds for the intersection of the camera frustum and the NavMeshSurface bounds
        public static Bounds CalculateAABBBoundsForCameraFrustumAndNavMeshSurfaceBounds(Camera camera, Bounds bounds)
        {
            var points = CalculateBoundsForCameraFrustumAndNavMeshSurfaceBounds(camera, bounds);
            var min = points.Count == 0 ? bounds.min : new Vector3(
                points.Select(p => p.x).Min(),
                points.Select(p => p.y).Min(),
                points.Select(p => p.z).Min());
            var max = points.Count == 0 ? bounds.max : new Vector3(
                points.Select(p => p.x).Max(),
                points.Select(p => p.y).Max(),
                points.Select(p => p.z).Max());
            return new Bounds((max + min) / 2, max - min);
        }

        // Calculate the vertices of the bounding volume for the intersection of the camera frustum and the NavMeshSurface bounds
        public static List<Vector3> CalculateBoundsForCameraFrustumAndNavMeshSurfaceBounds(Camera camera, Bounds bounds)
        {
            var points = new List<Vector3>();
            // Cast bounds to screen space in case the camera is viewing wider space than the bounds
            // Then use raycast to find the vertices
            var boundVertices = new Vector3[]
            {
                new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
                new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.max.y, bounds.max.z),
            };
            foreach (var vertex in boundVertices)
            {
                var screenPoint = camera.WorldToScreenPoint(vertex);
                if (screenPoint.z <= 0)
                    continue;
                if (screenPoint.x >= 0 && screenPoint.x <= camera.pixelWidth && screenPoint.y >= 0 && screenPoint.y <= camera.pixelHeight)
                {
                    points.Add(vertex);
                }

                if (!bounds.Contains(camera.transform.position))
                {
                    var p = new Vector3(
                        Mathf.Min(camera.pixelWidth, Mathf.Max(0, screenPoint.x)),
                        Mathf.Min(camera.pixelHeight, Mathf.Max(0, screenPoint.y)),
                        0);
                    var ray = camera.ScreenPointToRay(p);
                    var success = bounds.IntersectRay(ray, out var distance);
                    if (success)
                        points.Add(ray.GetPoint(distance));
                }
            }

            // If the camera is inside the bounds, include the four corners from the screen to decide the result
            if (bounds.Contains(camera.transform.position))
            {
                points.Add(camera.transform.position);
                var screenPoints = new Vector3[]
                {
                    new Vector3(0, 0, 0),
                    new Vector3(camera.pixelWidth, 0, 0),
                    new Vector3(0, camera.pixelHeight, 0),
                    new Vector3(camera.pixelWidth, camera.pixelHeight, 0)
                };
                foreach (var screenPoint in screenPoints)
                {
                    var ray = camera.ScreenPointToRay(screenPoint);
                    // If the camera is inside the box, the hit information will be the opposite direction of the ray
                    // Reference: https://answers.unity.com/questions/1546075/boundsintersectray-behavior-when-origin-is-inside.html
                    ray.direction = -ray.direction;
                    bounds.IntersectRay(ray, out var distance);
                    points.Add(ray.GetPoint(distance));
                }
            }

            return points;
        }
    }

}
