using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Serialization;

namespace Unity.CV.SyntheticHumans.Tags
{
    [CreateAssetMenu ( fileName = "NewBodyTag", menuName = "Synthetic Humans/Tags/Body" ) ]
    [Serializable]
    public class BodyTag : MeshTag
    {
        public SyntheticHumanHeightRange height;
        public SyntheticHumanWeightRange weight;
        public SyntheticHumanAgeRange age;
        [FormerlySerializedAs("sex")]
        public SyntheticHumanGender gender;
        public SyntheticHumanFileExtension fileType;
    }
}
