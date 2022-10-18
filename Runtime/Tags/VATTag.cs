using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.CV.SyntheticHumans.Tags
{
    [CreateAssetMenu(fileName = "NewVATTag", menuName = "Synthetic Humans/Tags/VAT")]
    [Serializable]
    public class VATTag : SyntheticHumanTag
    {
        [JsonProperty("meshpath")]
        public string parentMesh;
        //public Texture2D vatTex;     // The texture with the vertex differentials.
        public float vatmax; // The fitted maximum range of the vat.
        public float vatmin; // The fitted minimum range of the vat.

        public SyntheticHumanHeightRange height;
        public SyntheticHumanWeightRange weight;
        public SyntheticHumanAgeRange age;
        [FormerlySerializedAs("sex")]
        public SyntheticHumanGender gender;
        public SyntheticHumanEthnicity ethnicity; // Base Ethnicity
        public SyntheticHumanFileExtension fileType; // Image Type for VAT
        public SyntheticHumanFileExtension restGeoFileType; // Rest Geometry
        public SyntheticHumanElement element;

        // This will force the asset to load weights directly from the fbx rather than an external file.
        public bool loadWeightsFromMesh;

        // Deserialize this from a differently named property with the path to the weights in raw format.
        // TODO (CS): this is disabled for now as we're loading all weights from fbx until vatrig comes along.
        // As of July 28th, 2022, a lot of the functionality for supporting this has been removed, as it was causing
        // issues on import. Search for commits on this date to find the original implementation.
        // [JsonProperty("skinpath")]
        // public SkinWeights skinWeights;
    }
}
