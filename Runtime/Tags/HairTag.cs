using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.CV.SyntheticHumans.Tags
{
    [CreateAssetMenu ( fileName = "NewHairTag", menuName = "Synthetic Humans/Tags/Hair" ) ]
    [Serializable]
    public class HairTag : SyntheticHumanTag
    {
        //public int index;      // The specific Index to lookup.
        public int indexMax;   // The maximum number of lookups on the texture.
        //public Texture2D vatTex;     // The texture with the vertex differentials.
        public float vatMax;     // The fitted maximum range of the vat.
        public float vatMin;     // The fitted minimum range of the vat.

        public SyntheticHumanHeightRange height;
        public SyntheticHumanAgeRange age;
        public SyntheticHumanFileExtension fileType; // Rest Geometry
    }
}
