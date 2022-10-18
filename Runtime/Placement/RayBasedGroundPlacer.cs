using System;
using Unity.AI.Navigation;
using Unity.CV.SyntheticHumans.Tags;
using UnityEngine;

namespace Unity.CV.SyntheticHumans.Placement
{
    /// <summary>
    /// This ground placer could be used when the camera is placed inside rooms (under the ceilings) with walls
    /// </summary>
    [Serializable]
    class RayBasedGroundPlacer : GroundPlacer
    {
        public override string name => "Ray-Based Ground Placement";

        [AnimationPlacementGroupSerializedField]
        [Tooltip("The maximum horizontal angle (in degree) from the camera's forward direction for the ray-casting")]
        public float horizontalAngle = 45;

        [AnimationPlacementGroupSerializedField]
        [Tooltip("Use custom distances to decide the position to place human instead of using the Ray-Based Ground Placer Tag")]
        public bool overrideRayBasedGroundPlacerTag = false;

        [AnimationPlacementGroupSerializedField]
        [Tooltip("Minimum distance from the camera's position to place a human")]
        public float minimumDistance = 0;

        [AnimationPlacementGroupSerializedField]
        [SerializeField]
        [Tooltip("Maximum distance from the camera's position to place a human")]
        public float maximumDistance = 10;

        protected override bool SamplePositionOnNavMesh(Camera camera, NavMeshSurface surface, out Vector3 position)
        {
            var angle = s_RandomGenerator.NextFloat(-horizontalAngle, horizontalAngle);
            var direction = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
            var ray = new Ray(camera.transform.position, direction);

            var initialPosition = Vector3.zero;
            if (overrideRayBasedGroundPlacerTag)
            {
                initialPosition = ray.GetPoint(s_RandomGenerator.NextFloat(minimumDistance, maximumDistance));
            }
            else
            {
                var hit = RayBasedGroundPlacerTag.Raycast(ray, out var hitInfo);
                if (!hit)
                {
                    position = Vector3.zero;
                    return false;
                }
                initialPosition = ray.GetPoint(s_RandomGenerator.NextFloat(hitInfo.distance));
            }
            return SamplePositionOnNavMesh(initialPosition, surface, out position);
        }

        /// <inheritdoc/>
        protected override bool InCameraView(Bounds bounds, Camera camera, bool validateVisibility = false)
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

                var angle = Vector3.Angle(Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up),
                    Vector3.ProjectOnPlane(point - camera.transform.position, Vector3.up));
                if (angle > horizontalAngle)
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
    }
}
