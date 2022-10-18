using Unity.CV.SyntheticHumans.Tags;
using UnityEngine;

namespace Unity.CV.SyntheticHumans.Placement
{
    /// <summary>
    /// The placement tag used in the SittingPlacer. It attaches to the game objects
    /// that humans could sit on and defines the directions of the humans
    /// </summary>
    public class SittingPlacerTag : NavMeshPlacerTag
    {
        public Bounds volume = new Bounds(Vector3.zero, Vector3.one);
        public float minimumDirectionAngle = 0;
        public float maximumDirectionAngle = 360f;

        const float k_VolumeExpand = 0.01f;

        public void UpdateVolumeByRenderingMesh()
        {
            var bounds = new Bounds();
            var meshFilters = GetComponentsInChildren<MeshFilter>();
            foreach (var meshFilter in meshFilters)
            {
                var b = meshFilter.sharedMesh.bounds;
                var center = transform.InverseTransformPoint(meshFilter.transform.TransformPoint(b.center));
                var size = transform.InverseTransformVector(meshFilter.transform.TransformVector(b.size));
                bounds.Encapsulate(new Bounds(center, size + Vector3.one * k_VolumeExpand));
            }
            volume = bounds;
        }

        public float GetRadius()
        {
            var worldSize = transform.TransformVector(volume.size);
            return new Vector2(worldSize.x, worldSize.z).magnitude / 2;
        }
    }
}
