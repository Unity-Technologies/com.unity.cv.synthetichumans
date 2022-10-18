using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.CV.SyntheticHumans.Tags
{
    [CreateAssetMenu ( fileName = "NewAnimTag", menuName = "Synthetic Humans/Tags/Animation" ) ]
    public class AnimationTag : SyntheticHumanTag
    {
        public SyntheticHumanHeightRange height;
        public SyntheticHumanWeightRange weight;
        public SyntheticHumanAgeRange age;
        [FormerlySerializedAs("sex")]
        public SyntheticHumanGender gender;
        public SyntheticHumanFileExtension fileType;
    }
}
