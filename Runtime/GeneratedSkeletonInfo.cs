using System.Collections.Generic;
using UnityEngine;

namespace Unity.CV.SyntheticHumans
{
    class GeneratedSkeletonInfo
    {
        public GameObject SkeletonRoot;
        public Dictionary<string, int> BoneNameIndices = new Dictionary<string, int>();
        public Transform[] OrderedBones;
        public Matrix4x4[] InitialPoses;
    }
}
