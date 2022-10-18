using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using Unity.CV.SyntheticHumans.Tags;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.Randomization.Samplers;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Unity.CV.SyntheticHumans.Placement
{
    [Serializable]
    class SittingPlacer : SyntheticHumanPlacer
    {
        public override string name => "Sitting Placement";

        static HashSet<string> s_AnchorBodyPartNames = new HashSet<string>() {"hip", "leg_left", "leg_right"};
        static HashSet<string> s_CollisionBodyPartNames = new HashSet<string>()
            {"knee_left", "lower_leg_left", "ankle_left", "foot_left", "knee_right", "lower_leg_right", "ankle_right", "foot_right"};
        static Dictionary<MeshTopology, int[]> s_AnchorBodyPartIndices = new Dictionary<MeshTopology, int[]>();

        const string k_SittingAnchorJoint = "upper_leg_left";


        public override bool Place(GameObject target)
        {
            SetupPlacementRandomizer();
            if (placementRandomizer == null)
                return false;
            if (s_RandomGenerator.state == 0)
                s_RandomGenerator.state = SamplerState.NextRandomState();

            if (placementRandomizer.cameras.Count == 0)
            {
                Debug.LogError("Missing cameras in the NavMesh Placement randomizer");
                return false;
            }

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

            // Sample human object to the sitting position
            var success = SampleSittingPosition(surface, camera, out var position, out var rotation);
            if (!success)
                return false;

            // Transport human to sit on the NavMesh surface
            var anchorHeight = GetMinimumVertexHeightOfAnchorBodyParts(target);
            var anchorZShift = target.transform.InverseTransformPoint(
                target.GetComponentsInChildren<JointLabel>().First(c => c.name == k_SittingAnchorJoint).transform.position).z;
            target.transform.position = position - target.transform.forward * anchorZShift - new Vector3(0, anchorHeight, 0);
            target.transform.rotation = rotation;
            Physics.SyncTransforms();

            // Validate visibility and collisions
            var allowedCollisions = NavMeshPlacerTag.GetActivePlacerTags<SittingPlacerTag>()
                .Select(t => t.GetComponent<Collider>())
                .Where(c => c != null);
            var allHumanColliders = target.GetComponent<HumanColliderManager>().Colliders;
            var sittingHumanColliders = allHumanColliders.Where(c => s_CollisionBodyPartNames.Contains(c.name));
            success = PostValidation(target, camera, sittingHumanColliders, new Collider[0]);
            if (!success)
                return false;
            success = PostValidation(target, camera, allHumanColliders, allowedCollisions);
            if (success)
                AddNavMeshObstacle(target);
            return success;
        }

        bool SampleSittingPosition(NavMeshSurface surface, Camera camera, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            // Select a sitting object by the tag
            var bounds = PlacerUtility.CalculateAABBBoundsForCameraFrustumAndNavMeshSurfaceBounds(camera, surface.GetBounds());
            var tags = NavMeshPlacerTag.GetActivePlacerTags<SittingPlacerTag>()
                .Where(t => bounds.Contains(t.transform.position) &&
                            InCameraView(new Bounds(t.transform.TransformPoint(t.volume.center), t.transform.TransformVector(t.volume.size)), camera))
                .ToArray();
            if (tags.Length == 0)
            {
                Debug.LogWarning("Failed to find a game object with SittingPlacerTag in the camera view and the NavMesh bounds");
                return false;
            }

            var tag = tags[s_RandomGenerator.NextInt(tags.Length)];

            // Sample the sitting position
            var angle = s_RandomGenerator.NextFloat(tag.minimumDirectionAngle, tag.maximumDirectionAngle);
            var direction = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
            var ray = new Ray(tag.volume.center, -direction);
            tag.volume.IntersectRay(ray, out var distance);
            var initialPosition = tag.transform.TransformPoint(ray.GetPoint(distance));

            var areaMask = NavMeshPlacerTag.GetAreaMask(new[] {tag});
            var success = NavMesh.SamplePosition(initialPosition, out var hit, tag.GetRadius(), areaMask);
            if (!success)
            {
                Debug.LogWarning($"Failed to sample a random position on the NavMesh surface {surface.name} near object {tag.name}");
                return false;
            }

            success = NavMesh.FindClosestEdge(hit.position, out var edgeHit, areaMask);
            if (!success)
            {
                Debug.LogWarning($"Failed to sample a random position on the edge of the NavMesh surface {surface.name} near object {tag.name}");
                return false;
            }

            // Note: only support rotation along y-axis because edgeHit.normal sometimes return invalid angles around x- or z-axis (e.g. zAngle=180)
            // Note: shift the placement position towards the facing direction because the NavMesh edge is offset from the real edge by the agent's radius
            position = edgeHit.position + Vector3.ProjectOnPlane(-edgeHit.normal, Vector3.up).normalized * surface.GetBuildSettings().agentRadius;
            rotation = Quaternion.Euler(new Vector3(0, Quaternion.FromToRotation(Vector3.forward, -edgeHit.normal).eulerAngles.y, 0));
            return true;
        }

        static float GetMinimumVertexHeightOfAnchorBodyParts(GameObject target)
        {
            var mesh = new Mesh();
            target.GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh);
            var vertices = mesh.vertices;

            var spec = target.GetComponent<SingleHumanSpecification>();
            var topology = new MeshTopology()
            {
                Age = spec.age,
                Gender = spec.gender,
            };
            var meshPartitions = HumanBodyPartitioner.GetMeshPartitions(topology, mesh);
            if (!s_AnchorBodyPartIndices.ContainsKey(topology))
            {
                s_AnchorBodyPartIndices[topology] = target.GetComponent<HumanColliderManager>().SkeletonOrderedBones
                    .Select((t, i) => new {t, i})
                    .Where(tuple => s_AnchorBodyPartNames.Contains(tuple.t.name))
                    .Select(tuple => tuple.i).ToArray();
            }

            var minVertexHeight = float.MaxValue;
            foreach (var index in s_AnchorBodyPartIndices[topology])
            {
                var minY = meshPartitions[index].Vertices.Select(i => vertices[i].y).Min();
                minVertexHeight = Mathf.Min(minY, minVertexHeight);
            }
            return minVertexHeight;
        }
    }
}
