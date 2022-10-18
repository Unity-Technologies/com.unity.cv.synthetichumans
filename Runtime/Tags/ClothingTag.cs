using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.CV.SyntheticHumans.Tags
{
    [CreateAssetMenu ( fileName = "NewBodyTag", menuName = "Synthetic Humans/Tags/Clothing" ) ]
    public class ClothingTag : MeshTag
    {
        public SyntheticHumanHeightRange height;
        public SyntheticHumanAgeRange age;
        [FormerlySerializedAs("sex")]
        public SyntheticHumanGender gender;
        public SyntheticHumanEthnicity ethnicity;
        public SyntheticHumanFileExtension fileType; // Rest Geometry
        public SyntheticHumanClothingElement clothingElement;
        public SyntheticHumanMaterialType materialType;
        /// <summary>
        /// As an alternate to materialType, clothing with a matching materialId will use only materials that match.
        /// </summary>
        public string materialId;
        public SyntheticHumanEnumBool resizable;
        public SyntheticHumanClothingLayer layer; // One is closest to body, higher numbers rest on top
        public SyntheticHumanEnumBool attachedToJoint;
        public string jointName;
        public Vector3 rotationOffset;
    }


    [Serializable]
    public struct ClothingParameters
    {
        [FormerlySerializedAs("baseMeshTag")]
        public SyntheticHumanClothingElement clothingElement;
        public SyntheticHumanClothingLayer layer; // One is closest to body, higher numbers rest on top
        //public bool required;
    }


}
