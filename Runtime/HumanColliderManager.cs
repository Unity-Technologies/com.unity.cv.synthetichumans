using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.CV.SyntheticHumans
{
    public class CollisionInfo
    {
        public Collider HumanCollider;
        public Collider OtherCollider;
        public Vector3 PenetrationDirection;
        public float PenetrationDistance;
    }

    public class HumanColliderManager : MonoBehaviour
    {
        internal Transform[] SkeletonOrderedBones;
        internal List<MeshCollider> Colliders;

        internal void Initialize()
        {
            // Use renderer's bounds for Physics.OverlapBox in collision detections
            gameObject.GetComponent<SkinnedMeshRenderer>().updateWhenOffscreen = true;
            if (Colliders == null || Colliders.Count == 0)
            {
                AddCollidersInChildren();
            }
        }

        internal void RemoveColliders()
        {
            if (Colliders != null && Colliders.Count > 0)
            {
                foreach (var collider in Colliders)
                {
                    DestroyImmediate(collider);
                }
                Colliders = null;
            }
        }

        // Check if there are collisions on any part of the human body, but ignore certain colliders that are not part of the human body
        // Use Animator.Update(0) and Physics.SyncTransforms() to sync all colliders before calling this method
        public static bool HasCollision(GameObject human, IEnumerable<Collider> humanColliders, HashSet<Collider> ignoreOtherColliders)
        {
            var colliders = CollisionDetectionByOverlapBox(human).Where(c => !ignoreOtherColliders.Contains(c));
            foreach (var humanCollider in humanColliders)
            {
                foreach (var collider in colliders)
                {
                    var hasCollision = GetCollisionInfo(humanCollider, collider, out _);
                    if (hasCollision)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // Get detailed collisions with the whole human body
        // Use Animator.Update(0) and Physics.SyncTransforms() to sync all colliders before calling this method
        public static List<CollisionInfo> GetCollisions(GameObject human) =>
            GetCollisions(human, human.GetComponent<HumanColliderManager>().Colliders);

        // Get detailed collisions with the given human bodies
        // Use Animator.Update(0) and Physics.SyncTransforms() to sync all colliders before calling this method
        public static List<CollisionInfo> GetCollisions(GameObject human, IEnumerable<Collider> humanColliders)
        {
            var collisions = new List<CollisionInfo>();
            var colliders = CollisionDetectionByOverlapBox(human);

            foreach (var collider in colliders)
            {
                foreach (var humanCollider in humanColliders)
                {
                    var hasCollision = GetCollisionInfo(humanCollider, collider, out var collisionInfo);
                    if (hasCollision)
                    {
                        collisions.Add(collisionInfo);
                    }
                }
            }
            return collisions;
        }

        // Add colliders to joints with JointLabel components
        void AddCollidersInChildren()
        {
            Colliders = new List<MeshCollider>();

            // Get partitioned meshes
            var spec = gameObject.GetComponent<SingleHumanSpecification>();
            var renderer = GetComponent<SkinnedMeshRenderer>();
            var renderedMesh = new Mesh();
            renderer.BakeMesh(renderedMesh);
            renderedMesh.boneWeights = renderer.sharedMesh.boneWeights;
            var meshes = HumanBodyPartitioner.Partition(renderedMesh,
                new MeshTopology(){Age = spec.age, Gender = spec.gender});

            // Add mesh colliders to each body part
            for (var i = 0; i < SkeletonOrderedBones.Length; i++)
            {
                if (meshes.ContainsKey(i))
                {
                    var bone = SkeletonOrderedBones[i];
                    var collider = bone.gameObject.AddComponent<MeshCollider>();
                    meshes[i].name = bone.name;
                    meshes[i].vertices = meshes[i].vertices.Select(v => bone.InverseTransformPoint(transform.TransformPoint(v))).ToArray();
                    collider.sharedMesh = meshes[i];
                    collider.convex = true;
                    collider.isTrigger = false;
                    Colliders.Add(collider);
                }
            }
        }

        static Collider[] CollisionDetectionByOverlapBox(GameObject human)
        {
            var bounds = human.GetComponent<SkinnedMeshRenderer>().bounds;
            var colliders = human.GetComponent<HumanColliderManager>().Colliders;
            return Physics.OverlapBox(bounds.center, bounds.extents).Where(c => !colliders.Contains(c)).ToArray();
        }

        static bool GetCollisionInfo(Collider humanCollider, Collider collider, out CollisionInfo collisionInfo)
        {
            collisionInfo = new CollisionInfo {HumanCollider = humanCollider, OtherCollider = collider};
            return Physics.ComputePenetration(humanCollider, humanCollider.transform.position,
                humanCollider.transform.rotation, collider, collider.transform.position,
                collider.transform.rotation, out collisionInfo.PenetrationDirection, out collisionInfo.PenetrationDistance);
        }
    }
}
