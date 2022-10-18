using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Perception.GroundTruth;

namespace Unity.CV.SyntheticHumans
{
    class SyntheticHumanSkeletonTemplate
    {
        [Serializable]
        class SkeletonFileRawData
        {
            public JointTemplate.JointRawData[] data;
        }

        class JointTemplate
        {
            [Serializable]
            public struct JointRawData
            {
                public string group;
                public int[] points;

                public string parent;
                [CanBeNull]
                public float[] offset;
                [CanBeNull]
                public string avatarName;
                [CanBeNull]
                public string jointLabelsOverride;
                [CanBeNull]
                public float[] tPoseRotation;
            }

            public List<JointTemplate> Children = new List<JointTemplate>();
            public JointRawData Raw;

            public float SelfOcclusionDistance;
        }

        JointTemplate m_JointTemplateRoot;

        public SyntheticHumanSkeletonTemplate(IEnumerable<TextAsset> files, float jointSelfOcclusionDistance)
        {
            var joints = new Dictionary<string, JointTemplate>();
            var jsonSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };

            // Read all skeleton files and convert into JointTemplate objects
            foreach (var file in files)
            {
                try
                {
                    var skeleton = JsonConvert.DeserializeObject<SkeletonFileRawData>(file.text, jsonSettings);
                    foreach (var rawJoint in skeleton.data)
                    {
                        Assert.IsFalse(joints.ContainsKey(rawJoint.group), "Trying to add multiple joints of the same name.");

                        // TODO it's weird that self occlusion distance is specified globally, not in the skeleton config file itself.
                        joints[rawJoint.group] = new JointTemplate { Raw = rawJoint, SelfOcclusionDistance = jointSelfOcclusionDistance };
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error while processing skeleton file {file.name}: {e}");
                }
            }

            // Build parent tree structure using processed joints
            foreach (var joint in joints.Values)
            {
                var parent = joint.Raw.parent;
                if (parent == "root")
                {
                    Assert.IsNull(m_JointTemplateRoot, "Skeleton file contains multiple root joints.");
                    m_JointTemplateRoot = joint;
                }
                else
                {
                    Assert.IsTrue(joints.ContainsKey(parent),
                        $"Trying to use unknown joint {parent} as parent.");

                    joints[parent].Children.Add(joint);
                }
            }

            Assert.IsNotNull(m_JointTemplateRoot, "No root joint found while processing skeleton.");
        }

        /// <summary>
        /// Creates a new skeleton to fit the target mesh renderer.
        /// </summary>
        /// <param name="targetRenderer">The renderer to fit the skeleton to.</param>
        internal GeneratedSkeletonInfo CreateSkeleton(SkinnedMeshRenderer targetRenderer)
        {
            var skeletonRoot = new GameObject("root");
            var animator = targetRenderer.gameObject.GetComponent<Animator>();
            if (animator == null)
            {
                animator = targetRenderer.gameObject.AddComponent<Animator>();
            }

            var humanBones = new List<HumanBone>();
            var skeletonBones = new List<SkeletonBone>();

            // Create a set of new bones for the given mesh, matching the template we have stored.
            var joints = new List<(JointTemplate, GameObject)>();
            CreateJoints(m_JointTemplateRoot, skeletonRoot.transform, targetRenderer.sharedMesh.vertices, ref joints);

            // We perform skeleton creation at the origin, as our vertex positions are in world space. Afterwards,
            // we set the proper parent transform and zero it out so that the skeleton takes on the same transform as
            // its parent.
            var rootObject = targetRenderer.gameObject;
            skeletonRoot.transform.parent = rootObject.transform;

            // Construct bones and human description for the Avatar.
            // The root object must be the first element in the list of the skeleton bones
            skeletonBones.Add(new SkeletonBone
            {
                name = skeletonRoot.name,
                position = skeletonRoot.transform.localPosition,
                rotation = skeletonRoot.transform.localRotation,
                scale = skeletonRoot.transform.localScale,
            });
            CreateAvatarBones(joints, ref humanBones, ref skeletonBones);
            var jointObjects = new List<GameObject> { skeletonRoot };
            jointObjects.AddRange(joints.Select(j => j.Item2));

            // Construct Avatar from the newly created Avatar human and skeleton bones
            var avatarDescription = new HumanDescription
            {
                skeleton = skeletonBones.ToArray(),
                human = humanBones.ToArray()
            };
            animator.avatar = AvatarBuilder.BuildHumanAvatar(rootObject, avatarDescription);

            // Create GeneratedSkeletonInfo that caller can use to set SkinnedMeshRenderer bones and generate weights
            var skeletonInfo = new GeneratedSkeletonInfo();
            skeletonInfo.SkeletonRoot = skeletonRoot;
            skeletonInfo.OrderedBones = new Transform[jointObjects.Count];
            skeletonInfo.InitialPoses = new Matrix4x4[jointObjects.Count];

            var rootLocalToWorld = rootObject.transform.localToWorldMatrix;
            var initialPositions = new Vector3[jointObjects.Count()];
            for (var i = 0; i < jointObjects.Count; i++)
            {
                var jointObject = jointObjects[i];
                initialPositions[i] = jointObject.transform.position;
                skeletonInfo.BoneNameIndices[jointObject.name] = i;
                skeletonInfo.OrderedBones[i] = jointObject.transform;
                skeletonInfo.InitialPoses[i] = jointObject.transform.worldToLocalMatrix * rootLocalToWorld;
            }

            return skeletonInfo;
        }

        // Create joint objects using the same hierarchical structure of the joint templates
        static void CreateJoints(JointTemplate jointTemplate, Transform parent,
            Vector3[] meshVertices, ref List<(JointTemplate, GameObject)> joints)
        {
            var jointObject = new GameObject(jointTemplate.Raw.group);
            jointObject.transform.parent = parent;
            joints.Add((jointTemplate, jointObject));

            var jointLabel = jointObject.AddComponent<JointLabel>();
            jointLabel.selfOcclusionDistance = jointTemplate.SelfOcclusionDistance;
            jointLabel.overrideSelfOcclusionDistance = true;

            jointObject.transform.position = GetJointCenterPoint(jointTemplate, meshVertices);

            // These label overrides are only necessary for bones that should be labeled but are not standard
            // avatar system bones.
            if (jointTemplate.Raw.jointLabelsOverride != null)
            {
                jointLabel.labels.Add(jointTemplate.Raw.jointLabelsOverride);
            }

            // Recursively continue skeleton creation for all children
            foreach (var child in jointTemplate.Children)
            {
                CreateJoints(child, jointObject.transform, meshVertices, ref joints);
            }
        }

        // Create avatar skeleton bones and the mapping of human bones for the joints
        static void CreateAvatarBones(List<(JointTemplate jointTemplate, GameObject jointObject)> joints,
            ref List<HumanBone> avatarHumanBones, ref List<SkeletonBone> avatarSkeletonBones)
        {
            // Add bones specific to the Avatar / Mecanim animation retargeting system
            foreach (var joint in joints)
            {
                if (joint.jointTemplate.Raw.avatarName != null)
                {
                    var tPoseQuaternion = Quaternion.identity;
                    if (joint.jointTemplate.Raw.tPoseRotation != null)
                    {
                        tPoseQuaternion = new Quaternion(
                            joint.jointTemplate.Raw.tPoseRotation[0],
                            joint.jointTemplate.Raw.tPoseRotation[1],
                            joint.jointTemplate.Raw.tPoseRotation[2],
                            joint.jointTemplate.Raw.tPoseRotation[3]);
                    }

                    var humanBone = new HumanBone
                    {
                        humanName = joint.jointTemplate.Raw.avatarName,
                        boneName = joint.jointTemplate.Raw.group,
                    };
                    humanBone.limit.useDefaultValues = true;
                    avatarHumanBones.Add(humanBone);

                    avatarSkeletonBones.Add(new SkeletonBone()
                    {
                        name = joint.jointTemplate.Raw.group,
                        position = joint.jointObject.transform.localPosition,
                        rotation = tPoseQuaternion,
                        scale = joint.jointObject.transform.localScale,
                    });
                }
            }
        }

        static Vector3 GetJointCenterPoint(JointTemplate jointTemplate, Vector3[] meshVertices)
        {
            // Get the centroid of the referenced mesh vertices
            var centerPos = Vector3.zero;
            foreach (var pointIdx in jointTemplate.Raw.points)
            {
                var point = meshVertices[pointIdx];
                centerPos += point;
            }

            centerPos /= jointTemplate.Raw.points.Length;

            // Apply any specified centroid offset if necessary
            if (jointTemplate.Raw.offset != null)
            {
                centerPos.x += jointTemplate.Raw.offset[0];
                centerPos.y += jointTemplate.Raw.offset[1];
                centerPos.z += jointTemplate.Raw.offset[2];
            }

            return centerPos;
        }
    }
}
