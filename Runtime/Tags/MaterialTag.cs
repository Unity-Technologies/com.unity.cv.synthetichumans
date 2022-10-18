using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.CV.SyntheticHumans.Tags
{
    [CreateAssetMenu ( fileName = "NewBodyTag", menuName = "Synthetic Humans/Tags/Material" ) ]
    [Serializable]
    public class MaterialTag : SyntheticHumanTag
    {
        public bool isBase;

        public SyntheticHumanAgeRange age;
        [FormerlySerializedAs("sex")]
        public SyntheticHumanGender gender;
        public SyntheticHumanEthnicity ethnicity;
        public SyntheticHumanHeightRange height;
        public SyntheticHumanFileExtension fileType;
        public SyntheticHumanElement humanElement;
        public SyntheticHumanClothingElement clothingElement;
        public SyntheticHumanMaterialType materialType;
        public SyntheticHumanEyeColor eyeColor;
        /// <summary>
        /// As an alternate to materialType, clothing with a matching materialId will use only materials that match.
        /// </summary>
        public string materialId;

        public SyntheticHumanEnumBool allowPropertyRandomization;
    }
}
